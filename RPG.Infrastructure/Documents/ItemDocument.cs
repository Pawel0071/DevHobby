using RPG.Domain.Entities.Items;
using RPG.Domain.Enums;

namespace RPG.Infrastructure.Documents;

public class ItemDocument : IMongoDocument
{
    public static string CollectionName => "Items";
    
    // Keep for backward compatibility
    public static string ItemCollection { get; } = "Items";

    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string TypeCode { get; set; } = default!;
    public ItemRarity Rarity { get; set; }
    public int RequiredLevel { get; set; }
    public int StackSize { get; set; }

    public List<string> Tags { get; set; } = new();

    // Komponenty jako dane
    public Dictionary<StatsProperty, int>? Modifiers { get; set; }
    public int? SocketNo { get; set; }
    public List<Guid>? SkillIds { get; set; }
    public Guid? QuestId { get; set; }
    public Guid? StepId { get; set; }
}
