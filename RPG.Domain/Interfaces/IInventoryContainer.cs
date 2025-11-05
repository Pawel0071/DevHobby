using RPG.Domain.Common;
using RPG.Domain.Entities.Items;

namespace RPG.Domain.Interfaces;

public interface IInventoryContainer
{
    IList<InventorySlot> Inventory { get; set; }
    Item this[int inventoryNo] { get; set; }
    int Capacity { get; init;  }
    int FreeSpace { get; }
    bool IsFull { get; }
    int IndexOf(Item item);
    bool Contains( Item item);
}