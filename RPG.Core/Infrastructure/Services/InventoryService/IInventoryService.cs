using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Interfaces;


namespace RPG.Core.Infrastructure.Services.InventoryService;

public interface IInventoryService
{
    bool AddItem(IInventory inventory, Item item);
    bool RemoveItem(IInventory inventory, Item item);
    bool Contains(IInventory inventory, Item item);
    int FreeSpace(IInventory inventory);
    bool IsFull(IInventory inventory);
}