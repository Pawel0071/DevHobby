using RPG.Core.Domain.Entities.Common;

namespace RPG.Core.Domain.Interfaces;

public interface IInventory
{
    IList<Item> InventoryItems { get; set; }
    int Capacity { get; set; }
    int FreeSpace { get; }
    bool IsFull { get; }
    bool AddToInventory(Item item);
    bool RemoveFromInventory(Item item);
}