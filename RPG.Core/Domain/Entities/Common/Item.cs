namespace RPG.Core.Domain.Entities.Common;

public class Item
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public ItemType Type { get; set; }
    public Dictionary<string, int>? Modifiers { get; set; }
    public int RequiredLevel { get; set; }
}