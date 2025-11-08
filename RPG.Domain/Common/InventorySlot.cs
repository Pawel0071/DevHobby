using RPG.Domain.Entities.Items;

namespace RPG.Domain.Common;

public class InventorySlot
{
    public Item? Item { get; set; } = null;
    public int Quantity { get; set; } = 0;
    public bool IsEmpty => Item == null;
}
