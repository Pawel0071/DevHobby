using RPG.Domain.Common;
using RPG.Domain.Containers;
using RPG.Domain.Enums;

namespace RPG.Infrastructure.Documents;

public class ItemDocument
{
    public static string ItemCollection { get; } = "Items";

    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public int RequiredLevel { get; set; }
    public int StackSize { get; set; }
    public Dictionary<StatsProperty, int> Modifiers { get; set; } = new();

    public Item ToDomain()
    {
        if (!Enum.TryParse<ItemType>(Type, out var parsedType))
            throw new InvalidOperationException($"Invalid ItemType: '{Type}'");

        return new Item
        {
            Id = Id,
            Name = Name,
            Type = parsedType,
            RequiredLevel = RequiredLevel,
            StackSize = StackSize,
            Modifiers = new StatsContainer(Modifiers)
        };
    }

    public static ItemDocument FromDomain(Item item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Type = item.Type.ToString(),
        RequiredLevel = item.RequiredLevel,
        StackSize = item.StackSize,
        Modifiers = new Dictionary<StatsProperty, int>(item.Modifiers.Stats)
    };
}