using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Interfaces;

namespace RPG.Core.Infrastructure.Services.InventoryService;

public class InventoryService : IInventoryService
{
    public bool AddItem(IInventory inventory, Item item)
    {
        return inventory.AddToInventory(item);
    }

    public bool RemoveItem(IInventory inventory, Item item)
    {
        return inventory.RemoveFromInventory(item);
    }

    public bool Contains(IInventory inventory, Item item)
    {
        return inventory.InventoryItems.Contains(item);
    }

    public bool IsFull(IInventory inventory)
    {
        return inventory.IsFull;
    }

    public int FreeSpace(IInventory inventory)
    {
        return inventory.FreeSpace;
    }
}