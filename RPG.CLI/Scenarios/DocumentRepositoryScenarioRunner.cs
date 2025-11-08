using System.Numerics;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RPG.Domain.Common;
using RPG.Domain.Containers;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.MapObjects.MapObjectComponents;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Entities.Npcs.NpcComponents;
using RPG.Domain.Entities.Quests;
using RPG.Domain.Entities.Quests.QuestComponents;
using RPG.Domain.Entities.Skills;
using RPG.Domain.Entities.Skills.SkillComponents;
using RPG.Domain.Enums;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Helpers;
using RPG.Infrastructure.Interfaces;

namespace RPG.CLI.Scenarios;

/// <summary>
///     Executes end-to-end scenarios that exercise every <see cref="DocumentRepository"/> operation
///     across all entity/document mappings registered in <see cref="DocumentMappingRegistry"/>.
/// </summary>
internal sealed class DocumentRepositoryScenarioRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentRepositoryScenarioRunner> _logger;
    private readonly IReadOnlyList<IDocumentRepositoryScenario> _scenarios;

    public DocumentRepositoryScenarioRunner(
        IServiceProvider serviceProvider,
        ILogger<DocumentRepositoryScenarioRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _scenarios = DocumentRepositoryScenarioFactory.CreateAll();
    }

    public async Task<int> RunAsync(string? entityKey, CancellationToken cancellationToken)
    {
        var scenarios = string.IsNullOrWhiteSpace(entityKey)
            ? _scenarios
            : _scenarios.Where(s =>
                    string.Equals(s.Name, entityKey, StringComparison.OrdinalIgnoreCase) ||
                    s.Name.StartsWith(entityKey + ".", StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (scenarios.Count == 0)
        {
            _logger.Error($"No document repository scenario registered for entity key '{entityKey}'.");
            return 1;
        }

        foreach (var scenario in scenarios)
        {
            try
            {
                await scenario.ExecuteAsync(_serviceProvider, _logger, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Warn($"Scenario '{scenario.Name}' cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error($"Scenario '{scenario.Name}' failed: {ex.Message}", ex);
                return 1;
            }
        }

        _logger.Info($"Document repository scenarios completed successfully ({scenarios.Count}).");
        return 0;
    }
}

internal interface IDocumentRepositoryScenario
{
    string Name { get; }

    Task ExecuteAsync(
        IServiceProvider serviceProvider,
        ILogger<DocumentRepositoryScenarioRunner> logger,
        CancellationToken cancellationToken);
}

internal sealed class DocumentRepositoryScenario<TEntity, TDocument> : IDocumentRepositoryScenario
    where TEntity : class, IDomainEntity
    where TDocument : class, IMongoDocument
{
    private readonly Func<TEntity> _createEntity;
    private readonly Action<TEntity>? _mutateEntity;
    private readonly Action<TEntity, TDocument>? _assertDocument;
    private readonly Action<TEntity, TEntity>? _assertEntity;

    public DocumentRepositoryScenario(
        string name,
        Func<TEntity> createEntity,
        Action<TEntity>? mutateEntity = null,
        Action<TEntity, TDocument>? assertDocument = null,
        Action<TEntity, TEntity>? assertEntity = null)
    {
        Name = name;
        _createEntity = createEntity;
        _mutateEntity = mutateEntity;
        _assertDocument = assertDocument;
        _assertEntity = assertEntity;
    }

    public string Name { get; }

    public async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        ILogger<DocumentRepositoryScenarioRunner> logger,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var documentRepository = scopedProvider.GetRequiredService<IDocumentRepository>();
        var mongoRepository = scopedProvider.GetRequiredService<IMongoDocumentRepository>();
        var redisRepository = scopedProvider.GetRequiredService<IRedisDocumentRepository>();

        var entity = _createEntity();
        var entityId = entity.Id;

        // Ensure a clean slate for this entity id (only delete when documents exist)
        var existingMongo = await mongoRepository.GetByIdAsync<TDocument>(entityId, cancellationToken);
        if (existingMongo is not null)
        {
            await mongoRepository.DeleteAsync<TDocument>(entityId, cancellationToken);
        }

        var existingRedis = await redisRepository.GetByIdAsync<TDocument>(entityId, cancellationToken);
        if (existingRedis is not null)
        {
            await redisRepository.DeleteAsync<TDocument>(entityId, cancellationToken);
        }

        var countBefore = await documentRepository.CountAsync<TEntity>(cancellationToken);

        logger.Info($"[{Name}] Upserting entity {entityId}.");
        await documentRepository.UpsertAsync(entity, cancellationToken);

        await ValidateStateAsync("initial upsert");

        var fetched = await documentRepository.GetByIdAsync<TEntity>(entityId, cancellationToken)
                     ?? throw new InvalidOperationException($"[{Name}] GetByIdAsync returned null.");
        if (fetched.Id != entityId)
        {
            throw new InvalidOperationException($"[{Name}] GetByIdAsync returned unexpected entity.");
        }

        _assertEntity?.Invoke(entity, fetched);

        var allEntities = await documentRepository.GetAllAsync<TEntity>(cancellationToken);
        if (!allEntities.Any(e => e.Id == entityId))
        {
            throw new InvalidOperationException($"[{Name}] Entity missing from GetAllAsync results.");
        }

        var countAfter = await documentRepository.CountAsync<TEntity>(cancellationToken);
        if (countAfter != countBefore + 1)
        {
            throw new InvalidOperationException($"[{Name}] Count mismatch after upsert. Expected {countBefore + 1}, got {countAfter}.");
        }

        var batchSize = (int)Math.Clamp(countAfter, 1, 10);
        var batch = await documentRepository.GetBatchAsync<TEntity>(0, batchSize, cancellationToken);
        if (!batch.Any(e => e.Id == entityId))
        {
            throw new InvalidOperationException($"[{Name}] Entity missing from GetBatchAsync results.");
        }

        if (_mutateEntity is not null)
        {
            _mutateEntity(entity);
            logger.Info($"[{Name}] Re-upserting mutated entity {entityId}.");
            await documentRepository.UpsertAsync(entity, cancellationToken);
            await ValidateStateAsync("update");

            var countAfterUpdate = await documentRepository.CountAsync<TEntity>(cancellationToken);
            if (countAfterUpdate != countAfter)
            {
                throw new InvalidOperationException($"[{Name}] Count changed after update. Expected {countAfter}, got {countAfterUpdate}.");
            }

            var updatedEntity = await documentRepository.GetByIdAsync<TEntity>(entityId, cancellationToken)
                                ?? throw new InvalidOperationException($"[{Name}] Updated entity missing after GetByIdAsync.");
            _assertEntity?.Invoke(entity, updatedEntity);
        }

        logger.Info($"[{Name}] Deleting entity {entityId}.");
        var deleted = await documentRepository.DeleteAsync<TEntity>(entityId, cancellationToken);
        if (!deleted)
        {
            throw new InvalidOperationException($"[{Name}] DeleteAsync returned false.");
        }

        // Allow the persistence pipeline to finish deleting
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);

        var mongoAfterDelete = await mongoRepository.GetByIdAsync<TDocument>(entityId, cancellationToken);
        if (mongoAfterDelete is not null)
        {
            throw new InvalidOperationException($"[{Name}] Mongo document still present after delete.");
        }

        var redisAfterDelete = await redisRepository.GetByIdAsync<TDocument>(entityId, cancellationToken);
        if (redisAfterDelete is not null)
        {
            throw new InvalidOperationException($"[{Name}] Redis document still present after delete.");
        }

        var countAfterDelete = await documentRepository.CountAsync<TEntity>(cancellationToken);
        if (countAfterDelete != countBefore)
        {
            throw new InvalidOperationException($"[{Name}] Count mismatch after delete. Expected {countBefore}, got {countAfterDelete}.");
        }

        var fetchAfterDelete = await documentRepository.GetByIdAsync<TEntity>(entityId, cancellationToken);
        if (fetchAfterDelete is not null)
        {
            throw new InvalidOperationException($"[{Name}] Entity still retrievable after delete.");
        }

        logger.Info($"[{Name}] Scenario completed successfully.");

        async Task ValidateStateAsync(string stage)
        {
            var mongoDoc = await mongoRepository.GetByIdAsync<TDocument>(entityId, cancellationToken)
                          ?? throw new InvalidOperationException($"[{Name}] Mongo document missing after {stage}.");
            LogDocumentName("Mongo", mongoDoc);
            LogDocumentComponents("Mongo", mongoDoc);
            _assertDocument?.Invoke(entity, mongoDoc);

            var redisDoc = await redisRepository.GetByIdAsync<TDocument>(entityId, cancellationToken)
                          ?? throw new InvalidOperationException($"[{Name}] Redis document missing after {stage}.");
            LogDocumentName("Redis", redisDoc);
            LogDocumentComponents("Redis", redisDoc);
            _assertDocument?.Invoke(entity, redisDoc);
        }

        void LogDocumentName(string source, object document)
        {
            if (document is null)
            {
                return;
            }

            var name = TryExtractName(document);
            if (!string.IsNullOrWhiteSpace(name))
            {
                logger.Info($"[{Name}] {source} document name: {name}");
            }
        }

        static string? TryExtractName(object document)
        {
            var nameProperty = document.GetType().GetProperty("Name");
            if (nameProperty?.GetValue(document) is string text && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return null;
        }

        void LogDocumentComponents(string source, TDocument document)
        {
            var componentsText = document switch
            {
                ItemDocument itemDoc => DescribeItemComponents(itemDoc),
                MapObjectDocument mapObjectDoc => DescribeComponentDataList(mapObjectDoc.Components),
                NpcDocument npcDoc => DescribeComponentDataList(npcDoc.Components),
                SkillDocument skillDoc => DescribeComponentDataList(skillDoc.Components),
                QuestDocument questDoc => DescribeComponentDataList(questDoc.Components),
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(componentsText))
            {
                logger.Info($"[{Name}] {source} components: {componentsText}");
            }
        }

        static string DescribeItemComponents(ItemDocument document)
        {
            var components = new List<string>();

            if (document.Modifiers is { Count: > 0 })
            {
                components.Add("StatsComponent");
            }

            if (document.SocketNo.HasValue)
            {
                components.Add("SocketComponent");
            }

            if (document.SkillIds is { Count: > 0 })
            {
                components.Add("SkillGrantComponent");
            }

            if (document.QuestId.HasValue && document.StepId.HasValue)
            {
                components.Add("QuestItemComponent");
            }

            if (document.EquipmentSlots is { Count: > 0 } ||
                document.IsTwoHanded.HasValue ||
                document.SupportsDualWield.HasValue ||
                document.IsUniqueEquip.HasValue)
            {
                components.Add("EquippableComponent");
            }

            if (document.UsedInItemIds is { Count: > 0 })
            {
                components.Add("CraftMaterialComponent");
            }

            return components.Count == 0
                ? "(brak komponentów)"
                : string.Join(", ", components);
        }

        static string DescribeComponentDataList(IReadOnlyCollection<ComponentData>? components)
        {
            if (components is not { Count: > 0 })
            {
                return "(brak komponentów)";
            }

            var names = components
                .Select(component => string.IsNullOrWhiteSpace(component.Type) ? "(nieznany)" : component.Type)
                .ToList();

            return string.Join(", ", names);
        }
    }
}

internal static class DocumentRepositoryScenarioFactory
{
    public static IReadOnlyList<IDocumentRepositoryScenario> CreateAll()
    {
        return new List<IDocumentRepositoryScenario>
        {
            CreateCharacterScenario(),
            CreateItemScenario(),
            CreateItemCraftingScenario(),
            CreateSkillScenario(),
            CreateQuestScenario(),
            CreateNpcScenario(),
            CreateNpcCombatScenario(),
            CreatePlayerScenario(),
            CreateMapObjectScenario(),
            CreateWorldStateScenario()
        };
    }

    private static IDocumentRepositoryScenario CreateCharacterScenario()
    {
        return new DocumentRepositoryScenario<Character, CharacterDocument>(
            "character",
            createEntity: () =>
            {
                var sessionId = Guid.NewGuid();
                var helm = CreateTestItem("cli.helm", "CLI Helm of Focus", ItemRarity.Uncommon);
                var backpackItem = CreateTestItem("cli.potion", "CLI Potion Bundle");
                var bankItem = CreateTestItem("cli.bankgem", "CLI Bank Gem", ItemRarity.Rare);

                var skill = Skill.Create("CLI Battle Cry", "Boosts allies");
                skill.Tags = new HashSet<string> { "buff", "support" };
                var activationTime = DateTime.UtcNow;

                var character = new Character(sessionId, CharacterClass.Warrior)
                {
                    Id = Guid.NewGuid(),
                    Name = "CLI Test Character",
                    PlayerId = Guid.NewGuid(),
                    SessionId = sessionId,
                    Level = 10,
                    Experience = 5000,
                    ExperienceToNextLevel = 10000,
                    CurrentHealth = 120,
                    MaxHealth = 150,
                    CurrentResource = 60,
                    MaxResource = 80
                };

                character.BaseStats[StatsProperty.Strength] = 12;
                character.BaseStats[StatsProperty.Intelligence] = 8;
                character.ModifiedStats[StatsProperty.Strength] = 15;
                character.ModifiedStats[StatsProperty.Intelligence] = 10;

                character.Equipments[EquipmentSlot.Head] = helm;

                character.BackpackInventory[0].Item = backpackItem;
                character.BackpackInventory[0].Quantity = 3;

                character.BankStorage[1].Item = bankItem;
                character.BankStorage[1].Quantity = 1;

                character.Skills[skill] = SkillAvailability.Available;
                character.ActiveSkills[skill] = activationTime;

                return character;
            },
            mutateEntity: entity =>
            {
                entity.Level += 1;
                entity.Experience += 2500;
                entity.ExperienceToNextLevel = Math.Max(0, entity.ExperienceToNextLevel - 1000);

                if (entity.Equipments[EquipmentSlot.Head] is Item currentHelm)
                {
                    entity.Equipments[EquipmentSlot.Head] =
                        CreateTestItem("cli.helm.upgraded", $"{currentHelm.Name} +1", ItemRarity.Rare);
                }

                if (!entity.BackpackInventory[0].IsEmpty)
                {
                    entity.BackpackInventory[0].Quantity += 2;
                }

                if (!entity.BankStorage[1].IsEmpty)
                {
                    entity.BankStorage[1].Quantity += 1;
                }

                var skillEntry = entity.Skills.Keys.FirstOrDefault();
                if (skillEntry is not null)
                {
                    entity.Skills[skillEntry] = SkillAvailability.Learnt;
                    entity.ActiveSkills[skillEntry] = DateTime.UtcNow.AddMinutes(1);
                }
            },
            assertDocument: (entity, document) =>
            {
                if (!string.Equals(entity.Name, document.Name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Character name mismatch between entity and document.");
                }

                foreach (var (slot, item) in entity.Equipments.Where(kvp => kvp.Value is not null))
                {
                    if (!document.Equipment.TryGetValue(slot.ToString(), out var docItemId) || docItemId != item!.Id)
                    {
                        throw new InvalidOperationException($"Character equipment mismatch for slot {slot}.");
                    }
                }

                foreach (var slot in entity.BackpackInventory.Where(s => !s.IsEmpty))
                {
                    if (!document.Backpack.Any(d => d.ItemId == slot.Item!.Id && d.Quantity == slot.Quantity))
                    {
                        throw new InvalidOperationException("Character backpack slot missing in document.");
                    }
                }

                foreach (var slot in entity.BankStorage.Where(s => !s.IsEmpty))
                {
                    if (!document.Bank.Any(d => d.ItemId == slot.Item!.Id && d.Quantity == slot.Quantity))
                    {
                        throw new InvalidOperationException("Character bank slot missing in document.");
                    }
                }

                foreach (var (skill, availability) in entity.Skills)
                {
                    if (!document.Skills.TryGetValue(skill.Id.ToString(), out var docAvailability) || docAvailability != availability.ToString())
                    {
                        throw new InvalidOperationException("Character skill availability mismatch.");
                    }
                }

                foreach (var (skill, activeAt) in entity.ActiveSkills)
                {
                    if (!document.ActiveSkills.TryGetValue(skill.Id.ToString(), out var docActiveAt))
                    {
                        throw new InvalidOperationException("Character active skill timestamp missing.");
                    }

                    var delta = Math.Abs((docActiveAt - activeAt).TotalMilliseconds);
                    if (delta > 5)
                    {
                        throw new InvalidOperationException("Character active skill timestamp mismatch.");
                    }
                }
            });
    }

    private static IDocumentRepositoryScenario CreateItemScenario()
    {
        return new DocumentRepositoryScenario<Item, ItemDocument>(
            "item.socketed",
            createEntity: () =>
            {
                var grantedSkillId = Guid.NewGuid();
                var questId = Guid.NewGuid();
                var questStepId = Guid.NewGuid();

                var item = new Item(Guid.NewGuid(), "cli.sample")
                {
                    Name = "CLI Test Blade",
                    Rarity = ItemRarity.Rare,
                    RequiredLevel = 8,
                    StackSize = 1,
                    Tags = new HashSet<string> { "cli", "test" },
                    Components = new List<IItemComponent>
                    {
                        new StatsComponent
                        {
                            Stats = new StatsContainer(new Dictionary<StatsProperty, int>
                            {
                                { StatsProperty.Strength, 7 },
                                { StatsProperty.Vitality, 4 }
                            })
                        },
                        new SocketComponent { SocketNo = 2 },
                        new SkillGrantComponent { SkillIds = new List<Guid> { grantedSkillId } },
                        new QuestItemComponent { QuestId = questId, StepId = questStepId }
                    }
                };

                return item;
            },
            mutateEntity: entity =>
            {
                entity.StackSize = 5;
                entity.Rarity = ItemRarity.Legendary;

                if (entity.Components.OfType<StatsComponent>().FirstOrDefault() is { Stats: StatsContainer stats })
                {
                    stats[StatsProperty.Strength] += 3;
                    stats[StatsProperty.Vitality] += 1;
                }

                if (entity.Components.OfType<SocketComponent>().FirstOrDefault() is { } socketComponent)
                {
                    socketComponent.SocketNo += 1;
                }

                if (entity.Components.OfType<SkillGrantComponent>().FirstOrDefault() is { } skillGrant)
                {
                    skillGrant.SkillIds.Add(Guid.NewGuid());
                }
            },
            assertDocument: (entity, document) =>
            {
                if (!string.Equals(entity.Name, document.Name, StringComparison.Ordinal))
                    throw new InvalidOperationException("Item name mismatch between entity and document.");
                if (entity.StackSize != document.StackSize)
                    throw new InvalidOperationException("Item stack size mismatch between entity and document.");

                var statsComponent = entity.Components.OfType<StatsComponent>().FirstOrDefault();
                if (statsComponent is not null && statsComponent.Stats is StatsContainer stats)
                {
                    if (document.Modifiers is null)
                        throw new InvalidOperationException("Item stat modifiers missing in document.");

                    foreach (var kvp in stats.Stats)
                    {
                        if (!document.Modifiers.TryGetValue(kvp.Key.ToString(), out var docValue) || docValue != kvp.Value)
                            throw new InvalidOperationException($"Item stat modifier mismatch for {kvp.Key}.");
                    }
                }
                else if (document.Modifiers is { Count: > 0 })
                {
                    throw new InvalidOperationException("Unexpected modifiers present in item document.");
                }

                var socketComponent = entity.Components.OfType<SocketComponent>().FirstOrDefault();
                if (socketComponent is not null)
                {
                    if (document.SocketNo != socketComponent.SocketNo)
                        throw new InvalidOperationException("Item socket count mismatch.");
                }
                else if (document.SocketNo.HasValue)
                {
                    throw new InvalidOperationException("Unexpected socket count in document.");
                }

                var skillGrant = entity.Components.OfType<SkillGrantComponent>().FirstOrDefault();
                if (skillGrant is not null)
                {
                    if (document.SkillIds is null)
                        throw new InvalidOperationException("Item skill grants missing in document.");

                    if (!skillGrant.SkillIds.OrderBy(x => x).SequenceEqual(document.SkillIds.OrderBy(x => x)))
                        throw new InvalidOperationException("Item skill grants mismatch.");
                }
                else if (document.SkillIds is { Count: > 0 })
                {
                    throw new InvalidOperationException("Unexpected skill grants in document.");
                }

                var questItem = entity.Components.OfType<QuestItemComponent>().FirstOrDefault();
                if (questItem is not null)
                {
                    if (document.QuestId != questItem.QuestId || document.StepId != questItem.StepId)
                        throw new InvalidOperationException("Item quest linkage mismatch.");
                }
                else if (document.QuestId.HasValue || document.StepId.HasValue)
                {
                    throw new InvalidOperationException("Unexpected quest linkage in document.");
                }
            });
    }

    private static IDocumentRepositoryScenario CreateItemCraftingScenario()
    {
        return new DocumentRepositoryScenario<Item, ItemDocument>(
            "item.crafting",
            createEntity: () =>
            {
                var recipeTargets = new List<string>
                {
                    "cli.recipe.sword",
                    "cli.recipe.shield"
                };

                var item = new Item(Guid.NewGuid(), "cli.material")
                {
                    Name = "CLI Tempered Ingot",
                    Rarity = ItemRarity.Uncommon,
                    RequiredLevel = 12,
                    StackSize = 15,
                    Tags = new HashSet<string> { "cli", "crafting", "material" },
                    Components = new List<IItemComponent>
                    {
                        new EquippableComponent
                        {
                            ValidSlots = new List<EquipmentSlot> { EquipmentSlot.Weapon1, EquipmentSlot.Weapon2 },
                            IsTwoHanded = false,
                            SupportsDualWield = true,
                            IsUniqueEquip = false
                        },
                        new CraftMaterialComponent
                        {
                            UsedInItemIds = recipeTargets
                        }
                    }
                };

                return item;
            },
            mutateEntity: entity =>
            {
                entity.RequiredLevel += 3;
                entity.StackSize = Math.Max(entity.StackSize, 20);
                entity.Rarity = ItemRarity.Epic;

                if (entity.Components.OfType<CraftMaterialComponent>().FirstOrDefault() is { } craftMaterial)
                {
                    craftMaterial.UsedInItemIds.Add("cli.recipe.legendary-axe");
                }

                if (entity.Components.OfType<EquippableComponent>().FirstOrDefault() is { } equippable)
                {
                    var updatedEquippable = new EquippableComponent
                    {
                        ValidSlots = equippable.ValidSlots.Concat(new[] { EquipmentSlot.Head }).ToList(),
                        IsTwoHanded = true,
                        SupportsDualWield = false,
                        IsUniqueEquip = true
                    };

                    entity.Components.Remove(equippable);
                    entity.Components.Add(updatedEquippable);
                }
            },
            assertDocument: (entity, document) =>
            {
                if (document.EquipmentSlots is null)
                    throw new InvalidOperationException("Item equippable slots missing in document.");

                var equippable = entity.Components.OfType<EquippableComponent>().First();
                if (!document.EquipmentSlots.OrderBy(x => x).SequenceEqual(equippable.ValidSlots.OrderBy(x => x)))
                    throw new InvalidOperationException("Item equippable slots mismatch between entity and document.");

                if (document.IsTwoHanded != equippable.IsTwoHanded ||
                    document.SupportsDualWield != equippable.SupportsDualWield ||
                    document.IsUniqueEquip != equippable.IsUniqueEquip)
                {
                    throw new InvalidOperationException("Item equippable flags mismatch between entity and document.");
                }

                var craftMaterial = entity.Components.OfType<CraftMaterialComponent>().First();
                if (document.UsedInItemIds is null)
                    throw new InvalidOperationException("Item craft material targets missing in document.");

                if (!document.UsedInItemIds.OrderBy(x => x).SequenceEqual(craftMaterial.UsedInItemIds.OrderBy(x => x)))
                    throw new InvalidOperationException("Item craft material targets mismatch between entity and document.");
            },
            assertEntity: (expected, actual) =>
            {
                var expectedEquippable = expected.Components.OfType<EquippableComponent>().First();
                var actualEquippable = actual.Components.OfType<EquippableComponent>().FirstOrDefault()
                                     ?? throw new InvalidOperationException("Item equippable component missing after round-trip.");

                if (!actualEquippable.ValidSlots.OrderBy(x => x).SequenceEqual(expectedEquippable.ValidSlots.OrderBy(x => x)))
                    throw new InvalidOperationException("Item equippable slots mismatch after round-trip.");

                if (actualEquippable.IsTwoHanded != expectedEquippable.IsTwoHanded ||
                    actualEquippable.SupportsDualWield != expectedEquippable.SupportsDualWield ||
                    actualEquippable.IsUniqueEquip != expectedEquippable.IsUniqueEquip)
                {
                    throw new InvalidOperationException("Item equippable flags mismatch after round-trip.");
                }

                var expectedCraftMaterial = expected.Components.OfType<CraftMaterialComponent>().First();
                var actualCraftMaterial = actual.Components.OfType<CraftMaterialComponent>().FirstOrDefault()
                                        ?? throw new InvalidOperationException("Item craft material component missing after round-trip.");

                if (!actualCraftMaterial.UsedInItemIds.OrderBy(x => x).SequenceEqual(expectedCraftMaterial.UsedInItemIds.OrderBy(x => x)))
                    throw new InvalidOperationException("Item craft material targets mismatch after round-trip.");
            });
    }

    private static IDocumentRepositoryScenario CreateSkillScenario()
    {
        return new DocumentRepositoryScenario<Skill, SkillDocument>(
            "skill",
            createEntity: () =>
            {
                var skill = Skill.Create("CLI Flame", "Throws a burst of flame");
                skill.IconId = "icon_fire";
                skill.Tags = new HashSet<string> { "fire", "aoe" };

                var damageComponent = new DamageComponent
                {
                    BaseDamage = 120,
                    MinDamage = 100,
                    MaxDamage = 140,
                    ScalingFactor = 1.5f,
                    ScalingStat = "intellect",
                    DamageType = "fire",
                    CritMultiplier = 2.5f
                };

                var cooldownComponent = new CooldownComponent
                {
                    CooldownSeconds = 8,
                    UseGlobalCooldown = false,
                    MaxCharges = 2,
                    ChargeRecoverySeconds = 12,
                    SharedCooldownSkillIds = new List<Guid> { Guid.NewGuid() }
                };

                var resourceComponent = new ResourceCostComponent
                {
                    Costs = new Dictionary<string, int> { { "mana", 30 } },
                    GeneratesResources = new Dictionary<string, int> { { "heat", 5 } },
                    RefundOnInterrupt = false,
                    RefundPercentage = 50f
                };

                skill.Components.Add(damageComponent);
                skill.Components.Add(cooldownComponent);
                skill.Components.Add(resourceComponent);

                return skill;
            },
            mutateEntity: entity =>
            {
                entity.Description += " (empowered)";

                var damageComponent = entity.Components.OfType<DamageComponent>().First();
                damageComponent.BaseDamage += 40;
                damageComponent.MinDamage += 20;
                damageComponent.MaxDamage += 40;

                var cooldownComponent = entity.Components.OfType<CooldownComponent>().First();
                cooldownComponent.CooldownSeconds = Math.Max(1, cooldownComponent.CooldownSeconds - 2);
                cooldownComponent.SharedCooldownSkillIds.Add(Guid.NewGuid());

                var resourceComponent = entity.Components.OfType<ResourceCostComponent>().First();
                resourceComponent.Costs["mana"] += 10;
                resourceComponent.GeneratesResources["heat"] += 3;
            },
            assertDocument: (entity, document) =>
            {
                if (!string.Equals(entity.Description, document.Description, StringComparison.Ordinal))
                    throw new InvalidOperationException("Skill description mismatch between entity and document.");

                var damageDoc = DeserializeComponent<DamageComponent>(document.Components);
                var damageEntity = entity.Components.OfType<DamageComponent>().First();
                if (damageDoc.BaseDamage != damageEntity.BaseDamage ||
                    damageDoc.MinDamage != damageEntity.MinDamage ||
                    damageDoc.MaxDamage != damageEntity.MaxDamage ||
                    !string.Equals(damageDoc.DamageType, damageEntity.DamageType, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Skill damage component mismatch.");
                }

                var cooldownDoc = DeserializeComponent<CooldownComponent>(document.Components);
                var cooldownEntity = entity.Components.OfType<CooldownComponent>().First();
                if (cooldownDoc.CooldownSeconds != cooldownEntity.CooldownSeconds ||
                    cooldownDoc.MaxCharges != cooldownEntity.MaxCharges ||
                    cooldownDoc.UseGlobalCooldown != cooldownEntity.UseGlobalCooldown ||
                    !cooldownDoc.SharedCooldownSkillIds.OrderBy(x => x).SequenceEqual(cooldownEntity.SharedCooldownSkillIds.OrderBy(x => x)))
                {
                    throw new InvalidOperationException("Skill cooldown component mismatch.");
                }

                var resourceDoc = DeserializeComponent<ResourceCostComponent>(document.Components);
                var resourceEntity = entity.Components.OfType<ResourceCostComponent>().First();
                AssertDictionaryEqual(resourceEntity.Costs, resourceDoc.Costs, "Skill resource costs");
                AssertDictionaryEqual(resourceEntity.GeneratesResources, resourceDoc.GeneratesResources, "Skill generated resources");
            },
            assertEntity: (expected, actual) =>
            {
                var expectedDamage = expected.Components.OfType<DamageComponent>().First();
                var actualDamage = actual.Components.OfType<DamageComponent>().FirstOrDefault()
                                   ?? throw new InvalidOperationException("Skill damage component missing after round-trip.");
                if (actualDamage.BaseDamage != expectedDamage.BaseDamage ||
                    actualDamage.MaxDamage != expectedDamage.MaxDamage)
                {
                    throw new InvalidOperationException("Skill damage component mismatch after round-trip.");
                }

                var expectedCooldown = expected.Components.OfType<CooldownComponent>().First();
                var actualCooldown = actual.Components.OfType<CooldownComponent>().FirstOrDefault()
                                     ?? throw new InvalidOperationException("Skill cooldown component missing after round-trip.");
                if (actualCooldown.CooldownSeconds != expectedCooldown.CooldownSeconds ||
                    !actualCooldown.SharedCooldownSkillIds.OrderBy(x => x).SequenceEqual(expectedCooldown.SharedCooldownSkillIds.OrderBy(x => x)))
                {
                    throw new InvalidOperationException("Skill cooldown component mismatch after round-trip.");
                }

                var expectedResource = expected.Components.OfType<ResourceCostComponent>().First();
                var actualResource = actual.Components.OfType<ResourceCostComponent>().FirstOrDefault()
                                     ?? throw new InvalidOperationException("Skill resource component missing after round-trip.");
                AssertDictionaryEqual(expectedResource.Costs, actualResource.Costs, "Skill resource costs (entity)");
                AssertDictionaryEqual(expectedResource.GeneratesResources, actualResource.GeneratesResources, "Skill generated resources (entity)");
            });
    }

    private static IDocumentRepositoryScenario CreateQuestScenario()
    {
        return new DocumentRepositoryScenario<Quest, QuestDocument>(
            "quest",
            createEntity: () =>
            {
                var startLocation = Location.Create(10, 5, 0, Guid.NewGuid(), "map-1", "Zone Alpha");
                var turnInLocation = Location.Create(15, 7, 0, Guid.NewGuid(), "map-2", "Zone Beta");

                var quest = Quest.Create(
                    "CLI Quest",
                    "Collect the mysterious artifact.",
                    "Archivist Rhea",
                    startLocation,
                    new HashSet<string> { "story", "cli" });

                quest.TurnInLocation = turnInLocation;
                quest.QuestGiverId = Guid.NewGuid();

                quest.Components.Add(new KillObjectiveComponent
                {
                    TargetNpcId = Guid.NewGuid(),
                    TargetNpcName = "CLI Raider",
                    RequiredCount = 5,
                    CurrentCount = 0
                });

                quest.Components.Add(new LevelRequirementComponent { MinLevel = 8, MaxLevel = 20 });

                quest.Components.Add(new BasicRewardsComponent
                {
                    ExperienceReward = 1500,
                    GoldReward = 250
                });

                quest.Components.Add(new RepeatableQuestComponent
                {
                    CooldownHours = 6,
                    LastCompletedTime = DateTime.UtcNow.AddHours(-12)
                });

                return quest;
            },
            mutateEntity: entity =>
            {
                entity.Description = "Return with the artifact to Archivist Rhea.";
                var rewards = entity.Components.OfType<BasicRewardsComponent>().First();
                rewards.ExperienceReward += 250;
                rewards.GoldReward += 100;

                var killObjective = entity.Components.OfType<KillObjectiveComponent>().First();
                killObjective.RequiredCount += 2;

                var repeatable = entity.Components.OfType<RepeatableQuestComponent>().First();
                repeatable.LastCompletedTime = DateTime.UtcNow;
            },
            assertDocument: (entity, document) =>
            {
                if (!string.Equals(entity.Title, document.Title, StringComparison.Ordinal))
                    throw new InvalidOperationException("Quest title mismatch between entity and document.");
                if (document.TurnInLocation is null)
                    throw new InvalidOperationException("Quest turn-in location missing in document.");

                var killDoc = DeserializeComponent<KillObjectiveComponent>(document.Components);
                var killEntity = entity.Components.OfType<KillObjectiveComponent>().First();
                if (killDoc.RequiredCount != killEntity.RequiredCount || killDoc.TargetNpcId != killEntity.TargetNpcId)
                    throw new InvalidOperationException("Quest kill objective mismatch.");

                var levelDoc = DeserializeComponent<LevelRequirementComponent>(document.Components);
                var levelEntity = entity.Components.OfType<LevelRequirementComponent>().First();
                if (levelDoc.MinLevel != levelEntity.MinLevel || levelDoc.MaxLevel != levelEntity.MaxLevel)
                    throw new InvalidOperationException("Quest level requirement mismatch.");

                var rewardDoc = DeserializeComponent<BasicRewardsComponent>(document.Components);
                var rewardEntity = entity.Components.OfType<BasicRewardsComponent>().First();
                if (rewardDoc.ExperienceReward != rewardEntity.ExperienceReward ||
                    rewardDoc.GoldReward != rewardEntity.GoldReward)
                    throw new InvalidOperationException("Quest reward component mismatch.");

                var repeatableDoc = DeserializeComponent<RepeatableQuestComponent>(document.Components);
                var repeatableEntity = entity.Components.OfType<RepeatableQuestComponent>().First();
                if (repeatableDoc.CooldownHours != repeatableEntity.CooldownHours ||
                    repeatableDoc.LastCompletedTime != repeatableEntity.LastCompletedTime)
                    throw new InvalidOperationException("Quest repeatable component mismatch.");
            },
            assertEntity: (expected, actual) =>
            {
                var expectedKill = expected.Components.OfType<KillObjectiveComponent>().First();
                var actualKill = actual.Components.OfType<KillObjectiveComponent>().FirstOrDefault()
                                 ?? throw new InvalidOperationException("Quest kill component missing after round-trip.");
                if (actualKill.RequiredCount != expectedKill.RequiredCount)
                    throw new InvalidOperationException("Quest kill requirement mismatch after round-trip.");

                var expectedRewards = expected.Components.OfType<BasicRewardsComponent>().First();
                var actualRewards = actual.Components.OfType<BasicRewardsComponent>().FirstOrDefault()
                                    ?? throw new InvalidOperationException("Quest rewards component missing after round-trip.");
                if (actualRewards.GoldReward != expectedRewards.GoldReward ||
                    actualRewards.ExperienceReward != expectedRewards.ExperienceReward)
                    throw new InvalidOperationException("Quest rewards mismatch after round-trip.");

                var expectedRepeatable = expected.Components.OfType<RepeatableQuestComponent>().First();
                var actualRepeatable = actual.Components.OfType<RepeatableQuestComponent>().FirstOrDefault()
                                       ?? throw new InvalidOperationException("Quest repeatable component missing after round-trip.");
                if (actualRepeatable.LastCompletedTime != expectedRepeatable.LastCompletedTime)
                    throw new InvalidOperationException("Quest repeatable timestamp mismatch after round-trip.");
            });
    }

    private static IDocumentRepositoryScenario CreateNpcScenario()
    {
        return new DocumentRepositoryScenario<Npc, NpcDocument>(
            "npc.merchant",
            createEntity: () =>
            {
                var npc = Npc.Create(
                    "cli_npc",
                    "CLI Merchant",
                    Location.Create(0, 0, 0, Guid.NewGuid(), "market", "Central Plaza"),
                    Guid.NewGuid(),
                    new HashSet<string> { "merchant", "friendly" });

                npc.Level = 25;
                npc.Description = "A friendly merchant testing the document pipeline.";

                var merchantComponent = new MerchantComponent
                {
                    GoldAmount = 500,
                    GlobalPriceModifier = 0.9f,
                    PriceModifiers = new Dictionary<string, float>
                    {
                        { "potion", 0.8f },
                        { "weapon", 1.1f }
                    }
                };
                merchantComponent.MerchantInventory[0].Quantity = 5;

                var dialogueComponent = new DialogueComponent
                {
                    DialogueScript = "merchant_default",
                    GreetingText = "Welcome to my shop!",
                    FarewellText = "Come back anytime.",
                    ScriptParameters = new Dictionary<string, object>
                    {
                        { "questId", Guid.NewGuid().ToString() }
                    }
                };

                var questGiverComponent = new QuestGiverComponent
                {
                    AvailableQuests = new List<Guid> { Guid.NewGuid() }
                };

                npc.Components.Add(merchantComponent);
                npc.Components.Add(dialogueComponent);
                npc.Components.Add(questGiverComponent);

                return npc;
            },
            mutateEntity: entity =>
            {
                entity.Description += " Now offering discounts!";

                var merchant = entity.Components.OfType<MerchantComponent>().First();
                merchant.GoldAmount += 200;
                merchant.PriceModifiers["potion"] = 0.75f;

                var dialogue = entity.Components.OfType<DialogueComponent>().First();
                dialogue.GreetingText = "Special deals just for you!";

                var questGiver = entity.Components.OfType<QuestGiverComponent>().First();
                questGiver.AvailableQuests.Add(Guid.NewGuid());
            },
            assertDocument: (entity, document) =>
            {
                if (!string.Equals(entity.DisplayName, document.DisplayName, StringComparison.Ordinal))
                    throw new InvalidOperationException("NPC display name mismatch between entity and document.");

                var merchantDoc = DeserializeComponent<MerchantComponent>(document.Components);
                var merchantEntity = entity.Components.OfType<MerchantComponent>().First();
                if (merchantDoc.GoldAmount != merchantEntity.GoldAmount ||
                    Math.Abs(merchantDoc.GlobalPriceModifier - merchantEntity.GlobalPriceModifier) > 0.0001f)
                    throw new InvalidOperationException("NPC merchant component mismatch.");
                AssertDictionaryEqual(merchantEntity.PriceModifiers, merchantDoc.PriceModifiers, "NPC merchant price modifiers");

                var dialogueDoc = DeserializeComponent<DialogueComponent>(document.Components);
                var dialogueEntity = entity.Components.OfType<DialogueComponent>().First();
                if (!string.Equals(dialogueDoc.GreetingText, dialogueEntity.GreetingText, StringComparison.Ordinal))
                    throw new InvalidOperationException("NPC dialogue greeting mismatch.");
                if (!string.Equals(dialogueDoc.FarewellText, dialogueEntity.FarewellText, StringComparison.Ordinal))
                    throw new InvalidOperationException("NPC dialogue farewell mismatch.");
                if (!dialogueEntity.ScriptParameters.Keys.All(k => dialogueDoc.ScriptParameters.ContainsKey(k)))
                    throw new InvalidOperationException("NPC dialogue script parameters mismatch.");

                var questGiverDoc = DeserializeComponent<QuestGiverComponent>(document.Components);
                var questGiverEntity = entity.Components.OfType<QuestGiverComponent>().First();
                if (!questGiverDoc.AvailableQuests.OrderBy(x => x).SequenceEqual(questGiverEntity.AvailableQuests.OrderBy(x => x)))
                    throw new InvalidOperationException("NPC quest giver component mismatch.");
            },
            assertEntity: (expected, actual) =>
            {
                var expectedMerchant = expected.Components.OfType<MerchantComponent>().First();
                var actualMerchant = actual.Components.OfType<MerchantComponent>().FirstOrDefault()
                                     ?? throw new InvalidOperationException("NPC merchant component missing after round-trip.");
                if (actualMerchant.GoldAmount != expectedMerchant.GoldAmount)
                    throw new InvalidOperationException("NPC merchant gold mismatch after round-trip.");

                var expectedDialogue = expected.Components.OfType<DialogueComponent>().First();
                var actualDialogue = actual.Components.OfType<DialogueComponent>().FirstOrDefault()
                                     ?? throw new InvalidOperationException("NPC dialogue component missing after round-trip.");
                if (!string.Equals(actualDialogue.GreetingText, expectedDialogue.GreetingText, StringComparison.Ordinal))
                    throw new InvalidOperationException("NPC dialogue mismatch after round-trip.");

                var expectedQuestGiver = expected.Components.OfType<QuestGiverComponent>().First();
                var actualQuestGiver = actual.Components.OfType<QuestGiverComponent>().FirstOrDefault()
                                       ?? throw new InvalidOperationException("NPC quest giver component missing after round-trip.");
                if (!actualQuestGiver.AvailableQuests.OrderBy(x => x).SequenceEqual(expectedQuestGiver.AvailableQuests.OrderBy(x => x)))
                    throw new InvalidOperationException("NPC quest list mismatch after round-trip.");
            });
    }

    private static IDocumentRepositoryScenario CreateNpcCombatScenario()
    {
        return new DocumentRepositoryScenario<Npc, NpcDocument>(
            "npc.combat",
            createEntity: () =>
            {
                var npc = Npc.Create(
                    "cli_npc_combat",
                    "CLI Arena Champion",
                    Location.Create(15, 3, 0, Guid.NewGuid(), "arena", "Battle Pit"),
                    Guid.NewGuid(),
                    new HashSet<string> { "hostile", "boss", "trainer" });

                npc.Level = 40;
                npc.Description = "A combat trainer who doubles as an arena boss.";

                var combatComponent = new CombatComponent
                {
                    AggroRange = 18f,
                    LeashRange = 24f,
                    AiBehaviorScript = "aggressive-champion"
                };
                combatComponent.GetStatsContainer()[StatsProperty.Strength] = 55;
                combatComponent.GetStatsContainer()[StatsProperty.Vitality] = 60;

                var lootableComponent = new LootableComponent
                {
                    ExperienceReward = 1250,
                    GoldReward = 375
                };
                var lootContainer = lootableComponent.GetLootContainer();
                lootContainer.LootSlots[0].Item = CreateTestItem("cli.loot.amulet", "CLI Amulet of Resilience", ItemRarity.Rare);
                lootContainer.LootSlots[0].MinQuantity = 1;
                lootContainer.LootSlots[0].MaxQuantity = 1;
                lootContainer.LootSlots[0].DropChance = 0.5f;
                lootContainer.LootSlots[1].Item = CreateTestItem("cli.loot.sigil", "CLI Sigil of Guarding", ItemRarity.Epic);
                lootContainer.LootSlots[1].MinQuantity = 1;
                lootContainer.LootSlots[1].MaxQuantity = 1;
                lootContainer.LootSlots[1].DropChance = 0.25f;

                var trainerComponent = new TrainerComponent
                {
                    Specialization = "Defensive Combat"
                };

                npc.Components.Add(combatComponent);
                npc.Components.Add(lootableComponent);
                npc.Components.Add(trainerComponent);

                return npc;
            },
            mutateEntity: entity =>
            {
                entity.Description += " The crowd cheers louder each round.";
                entity.Level += 1;

                if (entity.Components.OfType<CombatComponent>().FirstOrDefault() is { } combat)
                {
                    combat.AggroRange += 2f;
                    combat.LeashRange += 1f;
                    combat.AiBehaviorScript = "aggressive-overdrive";

                    foreach (var statKey in combat.Stats.Keys.ToList())
                    {
                        combat.Stats[statKey] += 5;
                    }
                }

                if (entity.Components.OfType<LootableComponent>().FirstOrDefault() is { } lootable)
                {
                    lootable.ExperienceReward += 250;
                    lootable.GoldReward += 100;
                    var lootContainer = lootable.GetLootContainer();
                    lootContainer.LootSlots[2].Item = CreateTestItem("cli.loot.token", "CLI Arena Token", ItemRarity.Uncommon);
                    lootContainer.LootSlots[2].MinQuantity = 1;
                    lootContainer.LootSlots[2].MaxQuantity = 3;
                    lootContainer.LootSlots[2].DropChance = 0.8f;
                }

                if (entity.Components.OfType<TrainerComponent>().FirstOrDefault() is { } trainer)
                {
                    trainer.Specialization = "Advanced Defensive Combat";
                }
            },
            assertDocument: (entity, document) =>
            {
                var combatDoc = DeserializeComponent<CombatComponent>(document.Components);
                var combatEntity = entity.Components.OfType<CombatComponent>().First();
                if (Math.Abs(combatDoc.AggroRange - combatEntity.AggroRange) > 0.001f ||
                    Math.Abs(combatDoc.LeashRange - combatEntity.LeashRange) > 0.001f ||
                    !string.Equals(combatDoc.AiBehaviorScript, combatEntity.AiBehaviorScript, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("NPC combat component mismatch.");
                }

                var lootDoc = DeserializeComponent<LootableComponent>(document.Components);
                var lootEntity = entity.Components.OfType<LootableComponent>().First();
                if (lootDoc.ExperienceReward != lootEntity.ExperienceReward ||
                    lootDoc.GoldReward != lootEntity.GoldReward)
                {
                    throw new InvalidOperationException("NPC lootable component rewards mismatch.");
                }

                var trainerDoc = DeserializeComponent<TrainerComponent>(document.Components);
                var trainerEntity = entity.Components.OfType<TrainerComponent>().First();
                if (!string.Equals(trainerDoc.Specialization, trainerEntity.Specialization, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("NPC trainer specialization mismatch.");
                }

            },
            assertEntity: (expected, actual) =>
            {
                var expectedCombat = expected.Components.OfType<CombatComponent>().First();
                var actualCombat = actual.Components.OfType<CombatComponent>().FirstOrDefault()
                                  ?? throw new InvalidOperationException("NPC combat component missing after round-trip.");
                if (!string.Equals(actualCombat.AiBehaviorScript, expectedCombat.AiBehaviorScript, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("NPC combat AI script mismatch after round-trip.");
                }

                var expectedLoot = expected.Components.OfType<LootableComponent>().First();
                var actualLoot = actual.Components.OfType<LootableComponent>().FirstOrDefault()
                                 ?? throw new InvalidOperationException("NPC lootable component missing after round-trip.");
                if (actualLoot.ExperienceReward != expectedLoot.ExperienceReward ||
                    actualLoot.GoldReward != expectedLoot.GoldReward)
                {
                    throw new InvalidOperationException("NPC loot rewards mismatch after round-trip.");
                }

                var expectedTrainer = expected.Components.OfType<TrainerComponent>().First();
                var actualTrainer = actual.Components.OfType<TrainerComponent>().FirstOrDefault()
                                   ?? throw new InvalidOperationException("NPC trainer component missing after round-trip.");
                if (!string.Equals(actualTrainer.Specialization, expectedTrainer.Specialization, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("NPC trainer specialization mismatch after round-trip.");
                }

            });
    }

    private static IDocumentRepositoryScenario CreatePlayerScenario()
    {
        return new DocumentRepositoryScenario<Player, PlayerDocument>(
            "player",
            createEntity: () =>
            {
                var player = Player.Create("cli-player", "cli.player@example.com");
                player.IsOnline = true;
                player.BannedUntil = DateTime.UtcNow.AddHours(6);
                return player;
            },
            mutateEntity: entity =>
            {
                entity.IsBanned = true;
                entity.BannedUntil = DateTime.UtcNow.AddDays(1);
                entity.LastLoginAt = DateTime.UtcNow.AddMinutes(5);
            },
            assertDocument: (entity, document) =>
            {
                if (!string.Equals(entity.Username, document.Username, StringComparison.Ordinal))
                    throw new InvalidOperationException("Player username mismatch between entity and document.");
                if (!AreDatesClose(entity.BannedUntil, document.BannedUntil))
                    throw new InvalidOperationException("Player banned until mismatch.");
                if (document.IsBanned != entity.IsBanned)
                    throw new InvalidOperationException("Player ban state mismatch.");
            },
            assertEntity: (expected, actual) =>
            {
                if (actual.IsBanned != expected.IsBanned)
                    throw new InvalidOperationException("Player ban state mismatch after round-trip.");
                if (!AreDatesClose(expected.BannedUntil, actual.BannedUntil))
                    throw new InvalidOperationException("Player banned until mismatch after round-trip.");
            });
    }

    private static IDocumentRepositoryScenario CreateMapObjectScenario()
    {
        return new DocumentRepositoryScenario<MapObject, MapObjectDocument>(
            "mapobject",
            createEntity: () =>
            {
                var mapObject = MapObject.Create(
                    "cli_chest",
                    Location.Create(new Vector3(5, 0, -3), Guid.NewGuid(), "cave", "Hidden Vault"),
                    Guid.NewGuid(),
                    "hidden-vault");

                mapObject.Description = "A test chest used for document repository validation.";
                mapObject.Tags = new HashSet<string> { "container", "rare" };
                mapObject.RotationYaw = 45f;

                var containerComponent = new ContainerComponent(5);
                var container = containerComponent.GetContainer();
                container[0] = CreateTestItem("cli.container.item", "CLI Vault Relic", ItemRarity.Epic);

                var lockableComponent = new LockableComponent
                {
                    IsLocked = true,
                    RequiredKeyItemId = Guid.NewGuid().ToString(),
                    LockpickDifficulty = 7,
                    CanBeLockpicked = false
                };

                var portalComponent = new PortalComponent
                {
                    DestinationWorldId = Guid.NewGuid(),
                    DestinationZoneId = "zone-target",
                    DestinationLocation = Location.Create(new Vector3(25, 0, 12), Guid.NewGuid(), "map-teleport", "Arcane Hub"),
                    RequiresActivation = true,
                    IsActivated = false,
                    MinimumLevel = 15,
                    RequiredQuestIds = new List<Guid> { Guid.NewGuid() }
                };

                mapObject.Components.Add(containerComponent);
                mapObject.Components.Add(lockableComponent);
                mapObject.Components.Add(portalComponent);

                return mapObject;
            },
            mutateEntity: entity =>
            {
                entity.Description += " (opened)";
                var lockable = entity.Components.OfType<LockableComponent>().First();
                lockable.IsLocked = false;
                lockable.CanBeLockpicked = true;

                var portal = entity.Components.OfType<PortalComponent>().First();
                portal.IsActivated = true;
                portal.MinimumLevel += 5;
            },
            assertDocument: (entity, document) =>
            {
                if (!string.Equals(entity.Name, document.Name, StringComparison.Ordinal))
                    throw new InvalidOperationException("MapObject name mismatch between entity and document.");

                var containerComponentData = document.Components.FirstOrDefault(c => c.Type == nameof(ContainerComponent))
                                             ?? throw new InvalidOperationException("MapObject container component missing from document.");
                using (var containerJson = JsonDocument.Parse(containerComponentData.Data))
                {
                    var itemsElement = containerJson.RootElement.GetProperty("Items");
                    var hasExpectedItem = itemsElement.EnumerateArray().Any(slot =>
                        slot.TryGetProperty("Item", out var itemElement) &&
                        itemElement.ValueKind == JsonValueKind.Object &&
                        itemElement.TryGetProperty("Name", out var nameElement) &&
                        string.Equals(nameElement.GetString(), "CLI Vault Relic", StringComparison.Ordinal));

                    if (!hasExpectedItem)
                    {
                        throw new InvalidOperationException("MapObject container items missing expected entry.");
                    }
                }

                var lockableDoc = DeserializeComponent<LockableComponent>(document.Components);
                var lockableEntity = entity.Components.OfType<LockableComponent>().First();
                if (lockableDoc.IsLocked != lockableEntity.IsLocked || lockableDoc.CanBeLockpicked != lockableEntity.CanBeLockpicked)
                    throw new InvalidOperationException("MapObject lockable component mismatch.");

                var portalDoc = DeserializeComponent<PortalComponent>(document.Components);
                var portalEntity = entity.Components.OfType<PortalComponent>().First();
                if (portalDoc.IsActivated != portalEntity.IsActivated || portalDoc.MinimumLevel != portalEntity.MinimumLevel)
                    throw new InvalidOperationException("MapObject portal component mismatch.");
            },
            assertEntity: (expected, actual) =>
            {
                var expectedPortal = expected.Components.OfType<PortalComponent>().First();
                var actualPortal = actual.Components.OfType<PortalComponent>().FirstOrDefault()
                                   ?? throw new InvalidOperationException("MapObject portal component missing after round-trip.");
                if (actualPortal.IsActivated != expectedPortal.IsActivated || actualPortal.MinimumLevel != expectedPortal.MinimumLevel)
                    throw new InvalidOperationException("MapObject portal mismatch after round-trip.");

                var expectedLockable = expected.Components.OfType<LockableComponent>().First();
                var actualLockable = actual.Components.OfType<LockableComponent>().FirstOrDefault()
                                     ?? throw new InvalidOperationException("MapObject lockable component missing after round-trip.");
                if (actualLockable.IsLocked != expectedLockable.IsLocked || actualLockable.CanBeLockpicked != expectedLockable.CanBeLockpicked)
                    throw new InvalidOperationException("MapObject lockable mismatch after round-trip.");
            });
    }

    private static IDocumentRepositoryScenario CreateWorldStateScenario()
    {
        return new DocumentRepositoryScenario<WorldState, WorldStateDocument>(
            "worldstate",
            createEntity: () =>
            {
                var worldState = WorldState.Create(Guid.NewGuid(), "CLI World");
                worldState.LastUpdated = DateTime.UtcNow;
                return worldState;
            },
            mutateEntity: entity => entity.WorldName = entity.WorldName + " Prime",
            assertDocument: (entity, document) =>
            {
                if (!string.Equals(entity.WorldName, document.WorldName, StringComparison.Ordinal))
                    throw new InvalidOperationException("WorldState world name mismatch between entity and document.");
            });
    }

    private static Item CreateTestItem(string typeCode, string name, ItemRarity rarity = ItemRarity.Common)
    {
        return new Item(Guid.NewGuid(), typeCode)
        {
            Name = name,
            Rarity = rarity,
            RequiredLevel = 1,
            StackSize = 1,
            Tags = new HashSet<string> { "cli", "test" }
        };
    }

    private static T DeserializeComponent<T>(IEnumerable<ComponentData> components)
    {
        var componentData = components.FirstOrDefault(c => c.Type == typeof(T).Name)
                            ?? throw new InvalidOperationException($"Component {typeof(T).Name} missing in document.");
        return JsonSerializer.Deserialize<T>(componentData.Data)
               ?? throw new InvalidOperationException($"Component {typeof(T).Name} deserialization failed.");
    }

    private static void AssertDictionaryEqual<TKey, TValue>(IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual, string context)
        where TKey : notnull
    {
        if (expected.Count != actual.Count)
            throw new InvalidOperationException($"{context}: expected {expected.Count} entries but found {actual.Count}.");

        foreach (var kvp in expected)
        {
            if (!actual.TryGetValue(kvp.Key, out var actualValue) || !EqualityComparer<TValue>.Default.Equals(actualValue, kvp.Value))
                throw new InvalidOperationException($"{context}: mismatch for key '{kvp.Key}'.");
        }
    }

    private static bool AreDatesClose(DateTime? expected, DateTime? actual, double toleranceMilliseconds = 5)
    {
        if (expected.HasValue != actual.HasValue)
        {
            return false;
        }

        if (!expected.HasValue)
        {
            return true;
        }

        var delta = Math.Abs((expected.Value - actual!.Value).TotalMilliseconds);
        return delta <= toleranceMilliseconds;
    }
}
