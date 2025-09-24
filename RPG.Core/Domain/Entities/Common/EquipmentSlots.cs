namespace RPG.Core.Domain.Entities.Common;

public class EquipmentSlots
{
    public Item? Head { get; set; }
    public Item? Chest { get; set; }
    public Item? Weapon { get; set; }
    public Item? Shield { get; set; }
    public Item? Boots { get; set; }
    public Item? Gloves { get; set; }
    public List<Item>? Rings { get; set; } = [];
    public Item? Amulet { get; set; }
}