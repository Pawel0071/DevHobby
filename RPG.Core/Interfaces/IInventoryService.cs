using RPG.Core.Common;
using RPG.Domain.Interfaces;
using RPG.Domain.Models.Items;

namespace RPG.Core.Interfaces;

public interface IInventoryService
{
    ServiceResult<bool> AddItem(IInventoryContainer inventoryContainer, Item item);
    ServiceResult<bool> RemoveItem(IInventoryContainer inventoryContainer, Item item);
    ServiceResult<bool> Contains(IInventoryContainer inventoryContainer, Item item);
    ServiceResult<int> FreeSpace(IInventoryContainer inventoryContainer);
    ServiceResult<bool> IsFull(IInventoryContainer inventoryContainer);
}
