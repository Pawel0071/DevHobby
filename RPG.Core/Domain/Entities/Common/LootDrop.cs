namespace RPG.Core.Domain.Entities.Common;

public class LootDrop
{
    public required string ItemId { get; set; }
    public float DropChance { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
}