
using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Interfaces;

namespace RPG.Core.Domain.Entities;

public class Inventory(int capacity) : IInventory
{
    public IList<Item> InventoryItems { get; set; } = new List<Item>(capacity);
    public int Capacity { get; set; } = capacity;

    public bool IsFull => InventoryItems.Count >= Capacity;
    public int FreeSpace => Capacity - InventoryItems.Count;

    public bool AddToInventory(Item item)
    {
        if (IsFull) return false;
        InventoryItems.Add(item);
        return true;
    }

    public bool RemoveFromInventory(Item item)
    {
        return InventoryItems.Remove(item);
    }
}