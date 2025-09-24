namespace RPG.Core.Domain.Entities.Common;

public class Chest
{
    public string Name { get; set; } = "Chest";
    public Location Position { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public bool IsLocked { get; set; } = false;
}