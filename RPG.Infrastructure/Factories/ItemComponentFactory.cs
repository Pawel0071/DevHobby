using RPG.Domain.Containers;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Infrastructure.Documents;

namespace RPG.Infrastructure.Factories;

public static class ItemComponentFactory
{
    private static readonly Dictionary<Type, Func<ItemDocument, IItemComponent?>> Factories = new()
    {
        [typeof(StatsComponent)] = doc =>
            doc.Modifiers is { Count: > 0 }
                ? new StatsComponent { Stats = new StatsContainer(doc.Modifiers!) }
                : null,

        [typeof(SocketComponent)] = doc =>
            doc.SocketNo.HasValue
                ? new SocketComponent { SocketNo = doc.SocketNo.Value }
                : null,

        [typeof(SkillGrantComponent)] = doc =>
            doc.SkillIds is { Count: > 0 }
                ? new SkillGrantComponent { SkillIds = doc.SkillIds! }
                : null,

        [typeof(QuestItemComponent)] = doc =>
            doc.QuestId.HasValue && doc.StepId.HasValue
                ? new QuestItemComponent { QuestId = doc.QuestId.Value, StepId = doc.StepId.Value }
                : null
    };

    public static IItemComponent? Create(Type type, ItemDocument doc)
    {
        return Factories.TryGetValue(type, out var factory)
            ? factory(doc)
            : null;
    }
}