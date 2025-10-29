using RPG.Core.Services.InventoryService;
using RPG.Domain.Common;
using RPG.Domain.Interfaces;

namespace RPG.Core.Interfaces;

public interface IInventoryService
{
    InventoryResult AddItem(IInventoryContainer inventoryContainer, Item item);
    InventoryResult RemoveItem(IInventoryContainer inventoryContainer, Item item);
    bool Contains(IInventoryContainer inventoryContainer, Item item);
    int FreeSpace(IInventoryContainer inventoryContainer);
    bool IsFull(IInventoryContainer inventoryContainer);
}