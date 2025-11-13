using RPG.Domain.Models.Items;

namespace RPG.Domain.Common;

/// <summary>
///     Represents a single loot slot with item and drop chance.
///     Similar to InventorySlot but includes drop probability.
/// </summary>
public class LootSlot
{
    public Item? Item { get; set; } = null;
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 1;
    public float DropChance { get; set; } = 1.0f; // 0.0 - 1.0 (0% - 100%)

    public bool IsEmpty => Item == null;
}
