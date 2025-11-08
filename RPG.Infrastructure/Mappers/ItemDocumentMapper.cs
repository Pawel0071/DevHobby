using RPG.Domain.Common;
using RPG.Domain.Containers;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Domain.Enums;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between Item domain entity and ItemDocument
/// </summary>
public class ItemDocumentMapper : IDocumentMapper<Item, ItemDocument>
{
    private readonly ItemTypeDefinition? _itemTypeDefinition;
    private readonly ILogger<ItemDocumentMapper> _logger;

    public ItemDocumentMapper(ILogger<ItemDocumentMapper> logger, ItemTypeDefinition? itemTypeDefinition = null)
    {
        _itemTypeDefinition = itemTypeDefinition;
        _logger = logger;
    }

    public ItemDocument ToDocument(Item entity)
    {
        _logger.Debug($"Converting Item to ItemDocument. Id={entity.Id}, Type={entity.TypeCode}");

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

        _logger.Debug(
            $"ItemDocument created. Id={doc.Id}, Components mapped: Stats={doc.Modifiers?.Count > 0}, Sockets={doc.SocketNo > 0}, Skills={doc.SkillIds?.Count > 0}");
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

        if (_itemTypeDefinition != null)
        {
            var required = _itemTypeDefinition.RequiredComponents ?? Enumerable.Empty<Type>();
            var optional = _itemTypeDefinition.OptionalComponents ?? Enumerable.Empty<Type>();

            foreach (var type in required.Concat(optional))
            {
                var component = CreateComponent(type, document);
                if (component != null)
                    item.Components.Add(component);
            }
        }

        _logger.Debug($"Item domain entity created. Id={item.Id}, Components={item.Components.Count}");
        return item;
    }

    /// <summary>
    ///     Creates a component from ItemDocument based on component type.
    ///     Returns null if the document doesn't have required data for that component.
    ///     Example usage:
    ///     var component = ItemDocumentMapper.CreateComponent(typeof(StatsComponent), doc);
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

        return null;
    }
}
