using System.Numerics;

namespace RPG.Core.Domain.Entities.Common;

public class Chest
{
    public string Name { get; set; } = "Chest";
    public Vector3 Position { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public bool IsLocked { get; set; } = false;
}