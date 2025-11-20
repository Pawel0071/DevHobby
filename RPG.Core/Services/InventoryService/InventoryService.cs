using RPG.Core.Common;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Models.Items;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.InventoryService;

public class InventoryService(
    ILogger<InventoryService> logger) : IInventoryService
{
    public ServiceResult<bool> AddItem(IList<InventorySlot> container, Item item)
    {
        if (container == null)
        {
            logger.Warn("AddItem called with null inventory container.");
            return ErrorCodeDefinition.InvalidOperation.ToFail<bool>("Invalid inventory container.");
        }

        logger.Debug($"Attempting to add item '{item.Name}' to inventory.");

        var stackableSlot = container.FirstOrDefault(slot =>
            slot.Item?.Equals(item) == true && slot.Quantity < item.StackSize);

        if (stackableSlot is not null)
        {
            stackableSlot.Quantity++;
            logger.Info($"Stacked item '{item.Name}' in existing slot. New quantity: {stackableSlot.Quantity}.");
            return true.ToResult();
        }

        var emptySlot = container.FirstOrDefault(slot => slot.IsEmpty);
        if (emptySlot is not null)
        {
            emptySlot.Item = item;
            emptySlot.Quantity = 1;
            logger.Info($"Placed item '{item.Name}' in empty slot.");
            return true.ToResult();
        }

        logger.Warn($"Failed to add item '{item.Name}' — no free slot available.");
        return ErrorCodeDefinition.NoFreeSlot.ToFail<bool>("Brak wolnych slotów w ekwipunku.");
    }

    public ServiceResult<bool> RemoveItem(IList<InventorySlot> container, Item item)
    {
        logger.Debug($"Attempting to remove item '{item.Name}' from inventory.");

        var slot = container.FirstOrDefault(s => s.Item?.Equals(item) == true);
        if (slot is null)
        {
            logger.Warn($"Item '{item.Name}' not found in inventory.");
            return ErrorCodeDefinition.ItemNotFound.ToFail<bool>("Nie znaleziono przedmiotu w ekwipunku.");
        }

        slot.Quantity--;
        logger.Info($"Decreased quantity of item '{item.Name}' to {slot.Quantity}.");

        if (slot.Quantity <= 0)
        {
            slot.Item = null;
            slot.Quantity = 0;
            logger.Info($"Item '{item.Name}' fully removed from slot.");
        }

        return true.ToResult();
    }

    public ServiceResult<bool> Contains(IList<InventorySlot> container, Item item)
    {
        var result = container.Any(slot => slot.Item?.Equals(item) == true);
        logger.Debug($"Checked if inventory contains item '{item.Name}': {result}");
        return result.ToResult();
    }

    public ServiceResult<bool> IsFull(IList<InventorySlot> container)
    {
        var result = container.All(slot => !slot.IsEmpty && slot.Quantity >= slot.Item!.StackSize);
        logger.Debug($"Checked if inventory is full: {result}");
        return result.ToResult();
    }

    public ServiceResult<int> FreeSpace(IList<InventorySlot> container)
    {
        var count = container.Count(slot =>
            slot.IsEmpty || (slot.Item != null && slot.Quantity < slot.Item.StackSize));

        logger.Debug($"Calculated free space in inventory: {count} slots available.");
        return count.ToResult();
    }
}
