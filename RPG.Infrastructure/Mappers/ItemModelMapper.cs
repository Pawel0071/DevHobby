using RPG.Domain.Containers;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Domain.Enums;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Abstractions;

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between Item domain entity and ItemDocument
/// </summary>
public class ItemModelMapper : IModelMapper<Item, ItemDocument>
{
    private readonly ILogger<ItemModelMapper> _logger;

    public ItemModelMapper(ILogger<ItemModelMapper> logger)
    {
        _logger = logger;
    }

    public ItemDocument ToPersistence(Item entity)
    {
        _logger.Debug($"Converting Item to ItemDocument. Id={entity.Id}, Type={entity.TypeCode}");

        // merge derived tags before persisting
        var componentTypes = entity.Components.Select(c => c.GetType());
        var derived = TagComponentHelper.ResolveComponentTags(componentTypes, TagTarget.Item);
        foreach (var tag in derived) entity.Tags.Add(tag);

        var doc = new ItemDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            TypeCode = entity.TypeCode,
            Rarity = entity.Rarity,
            RequiredLevel = entity.RequiredLevel,
            StackSize = entity.StackSize,
            Tags = entity.Tags.ToList()
        };

        // Map components to document fields
        if (entity.GetComponent<StatsComponent>() is { } stats && stats.Stats is { } statContainer)
        {
            doc.Modifiers = statContainer.Stats.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value
            );
        }

        if (entity.GetComponent<SocketComponent>() is { } socket)
            doc.SocketNo = socket.SocketNo;

        if (entity.GetComponent<SkillGrantComponent>() is { } skills)
            doc.SkillIds = skills.SkillIds.ToList();

        if (entity.GetComponent<QuestItemComponent>() is { } quest)
        {
            doc.QuestId = quest.QuestId;
            doc.StepId = quest.StepId;
        }

        if (entity.GetComponent<EquippableComponent>() is { } equippable)
        {
            doc.EquipmentSlots = equippable.ValidSlots?.ToList();
            doc.IsTwoHanded = equippable.IsTwoHanded;
            doc.SupportsDualWield = equippable.SupportsDualWield;
            doc.IsUniqueEquip = equippable.IsUniqueEquip;
        }

        if (entity.GetComponent<CraftMaterialComponent>() is { } material)
        {
            doc.UsedInItemIds = material.UsedInItemIds?.ToList();
        }

        _logger.Debug(
            $"ItemDocument created. Id={doc.Id}, Components mapped: Stats={doc.Modifiers?.Count > 0}, Sockets={doc.SocketNo > 0}, Skills={doc.SkillIds?.Count > 0}, Equippable={doc.EquipmentSlots is { Count: > 0 }}, CraftMaterial={doc.UsedInItemIds is { Count: > 0 }}");
        return doc;
    }

    public Item ToDomain(ItemDocument document)
    {
        _logger.Debug($"Converting ItemDocument to Item. Id={document.Id}, Type={document.TypeCode}");

        var item = new Item(document.Id, document.TypeCode)
        {
            Name = document.Name,
            Rarity = document.Rarity,
            Tags = document.Tags != null ? new HashSet<string>(document.Tags) : new HashSet<string>(),
            Components = new List<IItemComponent>(),
            RequiredLevel = document.RequiredLevel,
            StackSize = document.StackSize
        };

        var requiredComponents = new HashSet<Type>();
        if (item.Tags.Count > 0)
        {
            // Centralized resolution from TagDefinition via TagComponentMap
            foreach (var type in TagComponentMap.GetRequiredComponentTypes(item.Tags, TagTarget.Item))
            {
                requiredComponents.Add(type);
            }
        }

        var presentComponents = new HashSet<Type>(GetTypesFromDocument(document));

        foreach (var type in requiredComponents.Concat(presentComponents))
        {
            if (item.Components.Any(component => component.GetType() == type))
            {
                continue;
            }

            var component = CreateComponent(type, document);
            if (component == null && requiredComponents.Contains(type))
            {
                component = CreateDefaultComponent(type);
            }

            if (component != null)
            {
                item.Components.Add(component);
            }
        }

        // merge derived tags from present components
        var tagsFromComponents = TagComponentHelper.ResolveComponentTags(item.Components.Select(c => c.GetType()), TagTarget.Item);
        foreach (var tag in tagsFromComponents) item.Tags.Add(tag);

        _logger.Debug($"Item domain entity created. Id={item.Id}, Components={item.Components.Count}");
        return item;
    }

    /// <summary>
    ///     Creates a component from ItemDocument based on component type.
    ///     Returns null if the document doesn't have required data for that component.
    ///     Example usage:
    ///     var component = ItemModelMapper.CreateComponent(typeof(StatsComponent), doc);
    ///     if (component != null) item.Components.Add(component);
    ///     Note: Not all tags require components - this method returns null if data is missing.
    /// </summary>
    public static IItemComponent? CreateComponent(Type type, ItemDocument doc)
    {
        if (type == typeof(StatsComponent) && doc.Modifiers is { Count: > 0 })
        {
            var parsed = new Dictionary<StatsProperty, int>();
            foreach (var (key, value) in doc.Modifiers!)
            {
                if (Enum.TryParse<StatsProperty>(key, out var stat))
                {
                    parsed[stat] = value;
                }
            }

            if (parsed.Count > 0)
            {
                return new StatsComponent
                {
                    Stats = new StatsContainer(parsed)
                };
            }

            return null;
        }

        if (type == typeof(SocketComponent) && doc.SocketNo.HasValue)
            return new SocketComponent { SocketNo = doc.SocketNo.Value };

        if (type == typeof(SkillGrantComponent) && doc.SkillIds is { Count: > 0 })
            return new SkillGrantComponent { SkillIds = new List<Guid>(doc.SkillIds!) };

        if (type == typeof(QuestItemComponent) && doc.QuestId.HasValue && doc.StepId.HasValue)
            return new QuestItemComponent { QuestId = doc.QuestId.Value, StepId = doc.StepId.Value };

        if (type == typeof(EquippableComponent))
        {
            if (doc.EquipmentSlots is null && doc.IsTwoHanded is null && doc.SupportsDualWield is null && doc.IsUniqueEquip is null)
            {
                return null;
            }

            return new EquippableComponent
            {
                ValidSlots = doc.EquipmentSlots?.ToList() ?? new List<EquipmentSlot>(),
                IsTwoHanded = doc.IsTwoHanded ?? false,
                SupportsDualWield = doc.SupportsDualWield ?? false,
                IsUniqueEquip = doc.IsUniqueEquip ?? false
            };
        }

        if (type == typeof(CraftMaterialComponent) && doc.UsedInItemIds is { Count: > 0 })
            return new CraftMaterialComponent { UsedInItemIds = new List<string>(doc.UsedInItemIds) };

        return null;
    }

    private static IEnumerable<Type> GetTypesFromDocument(ItemDocument doc)
    {
        if (doc.Modifiers is { Count: > 0 }) yield return typeof(StatsComponent);
        if (doc.SocketNo.HasValue) yield return typeof(SocketComponent);
        if (doc.SkillIds is { Count: > 0 }) yield return typeof(SkillGrantComponent);
        if (doc.QuestId.HasValue && doc.StepId.HasValue) yield return typeof(QuestItemComponent);
        if ((doc.EquipmentSlots is { Count: > 0 }) || doc.IsTwoHanded.HasValue || doc.SupportsDualWield.HasValue || doc.IsUniqueEquip.HasValue)
            yield return typeof(EquippableComponent);
        if (doc.UsedInItemIds is { Count: > 0 }) yield return typeof(CraftMaterialComponent);
    }

    private static IItemComponent? CreateDefaultComponent(Type type)
    {
        if (type == typeof(StatsComponent))
        {
            return new StatsComponent { Stats = new StatsContainer() };
        }

        if (type == typeof(SocketComponent))
        {
            return new SocketComponent();
        }

        if (type == typeof(SkillGrantComponent))
        {
            return new SkillGrantComponent();
        }

        if (type == typeof(QuestItemComponent))
        {
            return new QuestItemComponent();
        }

        if (type == typeof(EquippableComponent))
            return new EquippableComponent
            {
                ValidSlots = new List<EquipmentSlot>(),
                IsTwoHanded = false,
                SupportsDualWield = false,
                IsUniqueEquip = false
            };

        if (type == typeof(CraftMaterialComponent))
        {
            return new CraftMaterialComponent();
        }

        return Activator.CreateInstance(type) as IItemComponent;
    }
}
