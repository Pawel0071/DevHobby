using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Domain.Interfaces;

namespace RPG.Domain.Containers;

public class InventoryContainer(int capacity) : IInventoryContainer
{
    public IList<InventorySlot> Inventory { get; set; } = Enumerable.Range(0, capacity)
        .Select(_ => new InventorySlot())
        .ToList();

    public int Capacity { get; init; } = capacity;
    public bool IsFull => Inventory.Count >= Capacity;
    public int FreeSpace => Capacity - Inventory.Count;

    public Item this[int index]
    {
        get => Inventory[index].Item!;
        set => (Inventory[index].Item, Inventory[index].Quantity) =
            Inventory[index].Item switch
            {
                null => (value, 1),
                var i when i.Equals(value) => (i, Inventory[index].Quantity + 1),
                _ => throw new InvalidOperationException(
                    $"Slot {index} already contains '{Inventory[index].Item!.Name}'.")
            };
    }

    public int IndexOf(Item item)
    {
        return (int)(Inventory as List<InventorySlot>)?.FindIndex(slot => slot.Item?.Equals(item) == true)!;
    }

    public bool Contains(Item item)
    {
        return Inventory.Select(x => x.Item == item).Any();
    }
}
