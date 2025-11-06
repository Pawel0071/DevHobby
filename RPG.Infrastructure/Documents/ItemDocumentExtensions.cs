using RPG.Domain.Common;
using RPG.Domain.Containers;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Domain.Enums;

namespace RPG.Infrastructure.Documents;

public static class ItemDocumentExtensions
{
    public static Item ToDomain(this ItemDocument doc, ItemTypeDefinition? def)
    {
        var item = new Item(doc.Id, doc.TypeCode)
        {
            Name = doc.Name,
            Rarity = doc.Rarity,
            Tags = doc.Tags != null ? new HashSet<string>(doc.Tags) : new HashSet<string>(),
            Components = new List<IItemComponent>(),
            RequiredLevel = doc.RequiredLevel,
            StackSize = doc.StackSize,
        };

        if (def != null)
        {
            var required = def.RequiredComponents ?? Enumerable.Empty<Type>();
            var optional = def.OptionalComponents ?? Enumerable.Empty<Type>();
            foreach (var type in required.Concat(optional))
            {
                var component = CreateComponent(type, doc);
                if (component != null)
                    item.Components.Add(component);
            }
        }

        return item;
    }

    public static ItemDocument ToDocument(this Item item)
    {
        var doc = new ItemDocument
        {
            Id = item.Id,
            Name = item.Name,
            TypeCode = item.TypeCode,
            Rarity = item.Rarity,
            RequiredLevel = item.RequiredLevel,
            StackSize = item.StackSize,
            Tags = item.Tags.ToList()
        };

        if (item.GetComponent<StatsComponent>() is { } stats)
            if (stats.Stats != null)
                doc.Modifiers = new Dictionary<StatsProperty, int>(stats.Stats.Stats);

        if (item.GetComponent<SocketComponent>() is { } socket)
            doc.SocketNo = socket.SocketNo;

        if (item.GetComponent<SkillGrantComponent>() is { } skills)
            doc.SkillIds = skills.SkillIds.ToList();

        if (item.GetComponent<QuestItemComponent>() is { } quest)
        {
            doc.QuestId = quest.QuestId;
            doc.StepId = quest.StepId;
        }

        return doc;
    }
    private static IItemComponent? CreateComponent(Type type, ItemDocument doc)
    {
        return ComponentFactories.TryGetValue(type, out var factory)
            ? factory(doc)
            : null;
    }
    
    private static readonly Dictionary<Type, Func<ItemDocument, IItemComponent?>> ComponentFactories = new()
    {
        [typeof(StatsComponent)] = doc =>
            doc.Modifiers is { Count: > 0 }
                ? new StatsComponent { Stats = new StatsContainer(new Dictionary<StatsProperty,int>(doc.Modifiers!)) }
                : null,

        [typeof(SocketComponent)] = doc =>
            doc.SocketNo.HasValue
                ? new SocketComponent { SocketNo = doc.SocketNo.Value }
                : null,

        [typeof(SkillGrantComponent)] = doc =>
            doc.SkillIds is { Count: > 0 }
                ? new SkillGrantComponent { SkillIds = new List<Guid>(doc.SkillIds!) }
                : null,

        [typeof(QuestItemComponent)] = doc =>
            doc.QuestId.HasValue && doc.StepId.HasValue
                ? new QuestItemComponent { QuestId = doc.QuestId.Value, StepId = doc.StepId.Value }
                : null
    };
}