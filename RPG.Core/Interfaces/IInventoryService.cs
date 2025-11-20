using RPG.Core.Common;
using RPG.Domain.Common;
using RPG.Domain.Interfaces;
using RPG.Domain.Models.Items;

namespace RPG.Core.Interfaces;

public interface IInventoryService
{
    ServiceResult<bool> AddItem(IList<InventorySlot> inventoryContainer, Item item);
    ServiceResult<bool> RemoveItem(IList<InventorySlot> inventoryContainer, Item item);
    ServiceResult<bool> Contains(IList<InventorySlot> inventoryContainer, Item item);
    ServiceResult<int> FreeSpace(IList<InventorySlot> inventoryContainer);
    ServiceResult<bool> IsFull(IList<InventorySlot> inventoryContainer);
}
