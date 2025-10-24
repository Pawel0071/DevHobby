using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Interfaces;

namespace RPG.Core.Infrastructure.Services.InventoryService;

public class InventoryService : IInventoryService
{
    public InventoryResult AddItem(IInventoryContainer container, Item item)
    {
        var stackableSlot = container.Inventory.FirstOrDefault(slot =>
            slot.Item?.Equals(item) == true && slot.Quantity < item.StackSize);

        if (stackableSlot is not null)
        {
            stackableSlot.Quantity++;
            return InventoryResult.Ok();
        }
        
        var emptySlot = container.Inventory.FirstOrDefault(slot => slot.IsEmpty);
        if (emptySlot is not null)
        {
            emptySlot.Item = item;
            emptySlot.Quantity = 1;
            return InventoryResult.Ok();
        }

        return InventoryResult.Fail(InventoryError.NoFreeSlot, "Brak wolnych slotów w ekwipunku.");
    }

    public InventoryResult RemoveItem(IInventoryContainer container, Item item)
    {
        var slot = container.Inventory.FirstOrDefault(s => s.Item?.Equals(item) == true);
        if (slot is null)
            return InventoryResult.Fail(InventoryError.ItemNotFound, "Nie znaleziono przedmiotu w ekwipunku.");

        slot.Quantity--;
        if (slot.Quantity <= 0)
        {
            slot.Item = null;
            slot.Quantity = 0;
        }

        return InventoryResult.Ok();
    }

    public bool Contains(IInventoryContainer container, Item item) =>
        container.Inventory.Any(slot => slot.Item?.Equals(item) == true);

    public bool IsFull(IInventoryContainer container) =>
        container.Inventory.All(slot => !slot.IsEmpty && slot.Quantity >= slot.Item!.StackSize);

    public int FreeSpace(IInventoryContainer container) =>
        container.Inventory.Count(slot => slot.IsEmpty || (slot.Item != null && slot.Quantity < slot.Item.StackSize));
}