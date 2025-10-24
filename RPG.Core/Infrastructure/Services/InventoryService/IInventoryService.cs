using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Interfaces;


namespace RPG.Core.Infrastructure.Services.InventoryService;

public interface IInventoryService
{
    InventoryResult AddItem(IInventoryContainer inventoryContainer, Item item);
    InventoryResult RemoveItem(IInventoryContainer inventoryContainer, Item item);
    bool Contains(IInventoryContainer inventoryContainer, Item item);
    int FreeSpace(IInventoryContainer inventoryContainer);
    bool IsFull(IInventoryContainer inventoryContainer);
}