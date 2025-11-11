using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Entities.Npcs.NpcComponents;
using RPG.Domain.Entities.Skills;
using RPG.Domain.Entities.Skills.SkillComponents;
using RPG.Domain.Enums;

namespace RPG.WorldSeeder.Seeders;

internal sealed class SeedDataLoader
{
    private readonly ILogger<SeedDataLoader> _logger;
    private readonly string _rootPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public SeedDataLoader(IHostEnvironment environment, ILogger<SeedDataLoader> logger)
    {
        _logger = logger;

        var preferredRoot = Path.Combine(environment.ContentRootPath, "SeedData");
        _rootPath = Directory.Exists(preferredRoot)
            ? preferredRoot
            : Path.Combine(AppContext.BaseDirectory, "SeedData");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<SeedDataSet> LoadAsync(CancellationToken cancellationToken)
    {
        var items = await LoadItemsAsync(cancellationToken).ConfigureAwait(false);
        var skills = await LoadSkillsAsync(cancellationToken).ConfigureAwait(false);
        var mapObjects = await LoadMapObjectsAsync(cancellationToken).ConfigureAwait(false);
        var npcs = await LoadNpcsAsync(skills, items, cancellationToken).ConfigureAwait(false);
        var worldState = await LoadWorldStateAsync(cancellationToken).ConfigureAwait(false);

        return new SeedDataSet(items, skills, npcs, mapObjects, worldState);
    }

    private async Task<IReadOnlyDictionary<Guid, Item>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var folder = EnsureDirectory("Items");
        var result = new Dictionary<Guid, Item>();

        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var model = await DeserializeAsync<ItemSeedModel>(file, cancellationToken).ConfigureAwait(false);
            if (model is null)
            {
                continue;
            }

            var item = new Item(model.Id, model.TypeCode)
            {
                Name = model.Name,
                RequiredLevel = model.RequiredLevel,
                StackSize = model.StackSize,
                Tags = model.Tags?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>()
            };

            if (!string.IsNullOrWhiteSpace(model.Rarity) && Enum.TryParse<ItemRarity>(model.Rarity, true, out var rarity))
            {
                item.Rarity = rarity;
            }

            result[item.Id] = item;
        }

        _logger.LogInformation("Loaded {ItemCount} item definitions from seed data.", result.Count);
        return result;
    }

    private async Task<IReadOnlyDictionary<Guid, Skill>> LoadSkillsAsync(CancellationToken cancellationToken)
    {
        var folder = EnsureDirectory("Skills");
        var result = new Dictionary<Guid, Skill>();

        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var model = await DeserializeAsync<SkillSeedModel>(file, cancellationToken).ConfigureAwait(false);
            if (model is null)
            {
                continue;
            }

            var skill = Skill.Create(model.Name, model.Description ?? string.Empty);
            typeof(Skill).GetProperty("Id")!.SetValue(skill, model.Id);
            skill.IconId = model.IconId ?? string.Empty;
            skill.Tags = model.Tags?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
            skill.Components.Clear();

            if (model.Components != null)
            {
                foreach (var component in model.Components)
                {
                    var instance = CreateSkillComponent(component);
                    if (instance != null)
                    {
                        skill.Components.Add(instance);
                    }
                }
            }

            result[model.Id] = skill;
        }

        _logger.LogInformation("Loaded {SkillCount} skill definitions from seed data.", result.Count);
        return result;
    }

    private static ISkillComponent? CreateSkillComponent(SkillComponentSeedModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Type))
        {
            return null;
        }

        var type = model.Type.Trim().ToLowerInvariant();
        return type switch
        {
            "damage" => model.Properties.Deserialize<DamageComponent>(),
            "cooldown" => model.Properties.Deserialize<CooldownComponent>(),
            "requirement" => model.Properties.Deserialize<RequirementComponent>(),
            "movement" => model.Properties.Deserialize<MovementComponent>(),
            "buff" => model.Properties.Deserialize<BuffComponent>(),
            "healing" => model.Properties.Deserialize<HealingComponent>(),
            "healover" => model.Properties.Deserialize<HealOverTimeComponent>(),
            "damageovertime" => model.Properties.Deserialize<DamageOverTimeComponent>(),
            "shield" => model.Properties.Deserialize<ShieldComponent>(),
            "resourcecost" => model.Properties.Deserialize<ResourceCostComponent>(),
            "crowdcontrol" => model.Properties.Deserialize<CrowdControlComponent>(),
            "areaofeffect" => model.Properties.Deserialize<AreaOfEffectComponent>(),
            "casting" => model.Properties.Deserialize<CastingComponent>(),
            "combo" => model.Properties.Deserialize<ComboComponent>(),
            "debuff" => model.Properties.Deserialize<DebuffComponent>(),
            _ => null
        };
    }

    private async Task<IReadOnlyList<MapObject>> LoadMapObjectsAsync(CancellationToken cancellationToken)
    {
        var folder = EnsureDirectory("MapObjects");
        var result = new List<MapObject>();

        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var model = await DeserializeAsync<MapObjectSeedModel>(file, cancellationToken).ConfigureAwait(false);
            if (model is null)
            {
                continue;
            }

            var location = model.Location.ToDomain();
            var mapObject = MapObject.Create(model.Name, location, model.WorldId, model.ZoneId ?? string.Empty);
            typeof(MapObject).GetProperty("Id")!.SetValue(mapObject, model.Id);
            mapObject.DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Name : model.DisplayName;
            mapObject.Description = model.Description ?? string.Empty;
            mapObject.RotationYaw = model.RotationYaw;
            mapObject.IsActive = model.IsActive;
            mapObject.Tags = model.Tags?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
            mapObject.State = model.State != null
                ? new Dictionary<string, string>(model.State, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            mapObject.LastUpdated = model.LastUpdated ?? DateTime.UtcNow;

            result.Add(mapObject);
        }

        _logger.LogInformation("Loaded {MapObjectCount} map objects from seed data.", result.Count);
        return result;
    }

    private async Task<IReadOnlyList<Npc>> LoadNpcsAsync(
        IReadOnlyDictionary<Guid, Skill> skills,
        IReadOnlyDictionary<Guid, Item> items,
        CancellationToken cancellationToken)
    {
        var folder = EnsureDirectory("Npcs");
        var result = new List<Npc>();

        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var model = await DeserializeAsync<NpcSeedModel>(file, cancellationToken).ConfigureAwait(false);
            if (model is null)
            {
                continue;
            }

            var npc = Npc.Create(
                model.Name,
                model.DisplayName,
                model.SpawnLocation.ToDomain(),
                model.WorldId,
                model.Tags?.ToHashSet(StringComparer.OrdinalIgnoreCase));

            typeof(Npc).GetProperty("Id")!.SetValue(npc, model.Id);
            npc.Description = model.Description ?? string.Empty;
            npc.Level = model.Level;
            npc.IsAlive = model.IsAlive;
            npc.LastUpdated = model.LastUpdated ?? DateTime.UtcNow;
            npc.RespawnAt = model.RespawnAt;
            npc.Components.Clear();

            if (model.CurrentLocation != null)
            {
                npc.SetCurrentLocation(model.CurrentLocation.ToDomain());
            }

            if (model.Components != null)
            {
                foreach (var componentSeed in model.Components)
                {
                    var component = CreateNpcComponent(componentSeed, skills, items, model, npc);
                    if (component != null)
                    {
                        npc.Components.Add(component);
                    }
                }
            }

            result.Add(npc);
        }

        _logger.LogInformation("Loaded {NpcCount} NPCs from seed data.", result.Count);
        return result;
    }

    private INpcComponent? CreateNpcComponent(
        NpcComponentSeedModel model,
        IReadOnlyDictionary<Guid, Skill> skills,
        IReadOnlyDictionary<Guid, Item> items,
        NpcSeedModel context,
        Npc npc)
    {
        if (string.IsNullOrWhiteSpace(model.Type))
        {
            return null;
        }

        switch (model.Type.Trim().ToLowerInvariant())
        {
            case "dialogue":
            {
                var data = model.Properties.Deserialize<DialogueComponentSeedModel>(_jsonOptions);
                if (data == null)
                {
                    return null;
                }

                var component = new DialogueComponent
                {
                    DialogueScript = data.DialogueScript ?? string.Empty,
                    GreetingText = data.GreetingText ?? string.Empty,
                    FarewellText = data.FarewellText ?? string.Empty
                };

                if (data.ScriptParameters != null)
                {
                    foreach (var kvp in data.ScriptParameters)
                    {
                        component.ScriptParameters[kvp.Key] = kvp.Value.ValueKind == JsonValueKind.Number && kvp.Value.TryGetInt32(out var intValue)
                            ? intValue
                            : kvp.Value.ValueKind == JsonValueKind.True || kvp.Value.ValueKind == JsonValueKind.False
                                ? kvp.Value.GetBoolean()
                                : kvp.Value.ToString();
                    }
                }

                return component;
            }

            case "questgiver":
                return model.Properties.Deserialize<QuestGiverComponent>(_jsonOptions);

            case "trainer":
            {
                var data = model.Properties.Deserialize<TrainerComponentSeedModel>(_jsonOptions);
                if (data == null)
                {
                    return null;
                }

                var component = new TrainerComponent
                {
                    Specialization = data.Specialization ?? string.Empty
                };

                if (data.TeachableSkills != null)
                {
                    var container = component.GetSkillsContainer();
                    foreach (var entry in data.TeachableSkills)
                    {
                        if (!skills.TryGetValue(entry.SkillId, out var skill))
                        {
                            _logger.LogWarning(
                                "Trainer component on NPC {NpcName} references unknown skill {SkillId}.",
                                context.Name,
                                entry.SkillId);
                            continue;
                        }

                        if (!Enum.TryParse<SkillAvailability>(entry.Availability, true, out var availability))
                        {
                            availability = SkillAvailability.Available;
                        }

                        container.LearnSkill(skill, availability);
                    }
                }

                return component;
            }

            case "merchant":
            {
                var data = model.Properties.Deserialize<MerchantComponentSeedModel>(_jsonOptions);
                if (data == null)
                {
                    return null;
                }

                var component = new MerchantComponent
                {
                    GoldAmount = data.GoldAmount,
                    GlobalPriceModifier = data.GlobalPriceModifier,
                    PriceModifiers = data.PriceModifiers != null
                        ? new Dictionary<string, float>(data.PriceModifiers, StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                };

                if (data.Inventory != null)
                {
                    foreach (var slot in data.Inventory)
                    {
                        if (slot.Slot < 0 || slot.Slot >= component.MerchantInventory.Count)
                        {
                            _logger.LogWarning(
                                "Merchant component on NPC {NpcName} references invalid slot {Slot}.",
                                context.Name,
                                slot.Slot);
                            continue;
                        }

                        if (!items.TryGetValue(slot.ItemId, out var item))
                        {
                            _logger.LogWarning(
                                "Merchant component on NPC {NpcName} references unknown item {ItemId}.",
                                context.Name,
                                slot.ItemId);
                            continue;
                        }

                        component.MerchantInventory[slot.Slot].Item = item;
                        component.MerchantInventory[slot.Slot].Quantity = slot.Quantity;
                    }
                }

                return component;
            }

            case "combat":
            {
                var data = model.Properties.Deserialize<CombatComponentSeedModel>(_jsonOptions);
                if (data == null)
                {
                    return null;
                }

                var component = new CombatComponent
                {
                    AggroRange = data.AggroRange,
                    LeashRange = data.LeashRange,
                    AiBehaviorScript = data.AiBehaviorScript ?? string.Empty
                };

                if (data.Stats != null)
                {
                    var stats = component.GetStatsContainer();
                    foreach (var kvp in data.Stats)
                    {
                        if (!Enum.TryParse<StatsProperty>(kvp.Key, true, out var stat))
                        {
                            _logger.LogWarning(
                                "Combat component on NPC {NpcName} references unknown stat '{StatName}'.",
                                context.Name,
                                kvp.Key);
                            continue;
                        }

                        stats.Stats[stat] = kvp.Value;
                    }
                }

                if (data.Skills != null)
                {
                    var skillContainer = component.GetSkillsContainer();
                    foreach (var entry in data.Skills)
                    {
                        if (!skills.TryGetValue(entry.SkillId, out var skill))
                        {
                            _logger.LogWarning(
                                "Combat component on NPC {NpcName} references unknown skill {SkillId}.",
                                context.Name,
                                entry.SkillId);
                            continue;
                        }

                        if (!Enum.TryParse<SkillAvailability>(entry.Availability, true, out var availability))
                        {
                            availability = SkillAvailability.Available;
                        }

                        skillContainer.LearnSkill(skill, availability);
                    }
                }

                return component;
            }

            case "lootable":
            {
                var data = model.Properties.Deserialize<LootableComponentSeedModel>(_jsonOptions);
                if (data == null)
                {
                    return null;
                }

                var component = new LootableComponent
                {
                    ExperienceReward = data.ExperienceReward,
                    GoldReward = data.GoldReward
                };

                if (data.LootTable != null)
                {
                    var container = component.GetLootContainer();
                    foreach (var entry in data.LootTable)
                    {
                        if (entry.Slot < 0 || entry.Slot >= container.LootSlots.Count)
                        {
                            _logger.LogWarning(
                                "Lootable component on NPC {NpcName} references invalid loot slot {Slot}.",
                                context.Name,
                                entry.Slot);
                            continue;
                        }

                        if (!items.TryGetValue(entry.ItemId, out var item))
                        {
                            _logger.LogWarning(
                                "Lootable component on NPC {NpcName} references unknown item {ItemId}.",
                                context.Name,
                                entry.ItemId);
                            continue;
                        }

                        var slot = container.LootSlots[entry.Slot];
                        slot.Item = item;
                        slot.MinQuantity = entry.MinQuantity;
                        slot.MaxQuantity = entry.MaxQuantity;
                        slot.DropChance = entry.DropChance;
                    }
                }

                return component;
            }

            case "respawn":
            {
                var data = model.Properties.Deserialize<RespawnComponentSeedModel>(_jsonOptions);
                if (data == null)
                {
                    return null;
                }

                var component = new RespawnComponent
                {
                    RespawnTimeSeconds = data.RespawnTimeSeconds,
                    RespawnLocation = data.RespawnLocation?.ToDomain() ?? npc.SpawnLocation
                };

                return component;
            }

            default:
                _logger.LogWarning(
                    "NPC {NpcName} component type '{ComponentType}' is not supported by the seeding pipeline.",
                    context.Name,
                    model.Type);
                return null;
        }
    }

    private async Task<WorldState> LoadWorldStateAsync(CancellationToken cancellationToken)
    {
        var folder = EnsureDirectory("WorldState");
        var file = Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (file is null)
        {
            throw new InvalidOperationException("WorldState seed file was not found.");
        }

        var model = await DeserializeAsync<WorldStateSeedModel>(file, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("WorldState seed file is empty or invalid.");

        var world = WorldState.Hydrate(
            model.Id,
            model.WorldId,
            model.WorldName,
            model.LastUpdated ?? DateTime.UtcNow,
            model.Characters,
            model.Npcs,
            model.MapObjects);

        return world;
    }

    private string EnsureDirectory(string relative)
    {
        var path = Path.Combine(_rootPath, relative);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Seed data directory '{path}' was not found.");
        }

        return path;
    }

    private async Task<T?> DeserializeAsync<T>(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed record SeedDataSet(
    IReadOnlyDictionary<Guid, Item> Items,
    IReadOnlyDictionary<Guid, Skill> Skills,
    IReadOnlyList<Npc> Npcs,
    IReadOnlyList<MapObject> MapObjects,
    WorldState WorldState);

internal sealed class ItemSeedModel
{
    public Guid Id { get; init; }
    public string TypeCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Rarity { get; init; }
    public int RequiredLevel { get; init; }
    public int StackSize { get; init; }
    public List<string>? Tags { get; init; }
}

internal sealed class SkillSeedModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? IconId { get; init; }
    public List<string>? Tags { get; init; }
    public List<SkillComponentSeedModel>? Components { get; init; }
}

internal sealed class SkillComponentSeedModel
{
    public string Type { get; init; } = string.Empty;
    public JsonElement Properties { get; init; }
}

internal sealed class MapObjectSeedModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public LocationSeedModel Location { get; init; } = new();
    public Guid WorldId { get; init; }
    public string? ZoneId { get; init; }
    public bool IsActive { get; init; } = true;
    public float RotationYaw { get; init; }
    public HashSet<string>? Tags { get; init; }
    public Dictionary<string, string>? State { get; init; }
    public DateTime? LastUpdated { get; init; }
}

internal sealed class NpcSeedModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Level { get; init; }
    public Guid WorldId { get; init; }
    public LocationSeedModel SpawnLocation { get; init; } = new();
    public LocationSeedModel? CurrentLocation { get; init; }
    public HashSet<string>? Tags { get; init; }
    public bool IsAlive { get; init; } = true;
    public DateTime? RespawnAt { get; init; }
    public DateTime? LastUpdated { get; init; }
    public List<NpcComponentSeedModel>? Components { get; init; }
}

internal sealed class NpcComponentSeedModel
{
    public string Type { get; init; } = string.Empty;
    public JsonElement Properties { get; init; }
}

internal sealed class DialogueComponentSeedModel
{
    public string? DialogueScript { get; init; }
    public string? GreetingText { get; init; }
    public string? FarewellText { get; init; }
    public Dictionary<string, JsonElement>? ScriptParameters { get; init; }
}

internal sealed class TrainerComponentSeedModel
{
    public string? Specialization { get; init; }
    public List<TrainerSkillEntrySeedModel>? TeachableSkills { get; init; }
}

internal sealed class TrainerSkillEntrySeedModel
{
    public Guid SkillId { get; init; }
    public string Availability { get; init; } = SkillAvailability.Available.ToString();
}

internal sealed class MerchantComponentSeedModel
{
    public int GoldAmount { get; init; }
    public float GlobalPriceModifier { get; init; } = 1.0f;
    public Dictionary<string, float>? PriceModifiers { get; init; }
    public List<MerchantInventoryEntrySeedModel>? Inventory { get; init; }
}

internal sealed class MerchantInventoryEntrySeedModel
{
    public int Slot { get; init; }
    public Guid ItemId { get; init; }
    public int Quantity { get; init; } = 1;
}

internal sealed class CombatComponentSeedModel
{
    public float AggroRange { get; init; }
    public float LeashRange { get; init; }
    public string? AiBehaviorScript { get; init; }
    public Dictionary<string, int>? Stats { get; init; }
    public List<CombatSkillEntrySeedModel>? Skills { get; init; }
}

internal sealed class CombatSkillEntrySeedModel
{
    public Guid SkillId { get; init; }
    public string Availability { get; init; } = SkillAvailability.Available.ToString();
}

internal sealed class LootableComponentSeedModel
{
    public int ExperienceReward { get; init; }
    public int GoldReward { get; init; }
    public List<LootSlotSeedModel>? LootTable { get; init; }
}

internal sealed class LootSlotSeedModel
{
    public int Slot { get; init; }
    public Guid ItemId { get; init; }
    public int MinQuantity { get; init; } = 1;
    public int MaxQuantity { get; init; } = 1;
    public float DropChance { get; init; } = 1.0f;
}

internal sealed class RespawnComponentSeedModel
{
    public int RespawnTimeSeconds { get; init; } = 300;
    public LocationSeedModel? RespawnLocation { get; init; }
}

internal sealed class WorldStateSeedModel
{
    public Guid Id { get; init; }
    public Guid WorldId { get; init; }
    public string WorldName { get; init; } = string.Empty;
    public DateTime? LastUpdated { get; init; }
    public List<Guid>? Characters { get; init; }
    public List<Guid>? Npcs { get; init; }
    public List<Guid>? MapObjects { get; init; }
}

internal sealed class LocationSeedModel
{
    public VectorSeedModel Position { get; init; } = new();
    public Guid? WorldId { get; init; }
    public string MapId { get; init; } = string.Empty;
    public string ZoneName { get; init; } = string.Empty;
    public float Rotation { get; init; }

    public Location ToDomain()
    {
        var worldId = WorldId ?? Guid.Empty;
        var location = Location.Create(Position.ToVector3(), worldId, MapId, ZoneName);
        location.Rotation = Rotation;
        return location;
    }
}

internal sealed class VectorSeedModel
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }

    public System.Numerics.Vector3 ToVector3() => new(X, Y, Z);
}
