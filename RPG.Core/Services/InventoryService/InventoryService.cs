using RPG.Core.Common;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.InventoryService;

public class InventoryService : IInventoryService
{
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(ILogger<InventoryService> logger)
    {
        _logger = logger;
    }

    public ServiceResult<bool> AddItem(IInventoryContainer container, Item item)
    {
        if (container == null)
        {
            _logger.Warn("AddItem called with null inventory container.");
            return ErrorCodeDefinition.InvalidOperation.ToFail<bool>("Invalid inventory container.");
        }

        _logger.Debug($"Attempting to add item '{item.Name}' to inventory.");

        var stackableSlot = container.Inventory.FirstOrDefault(slot =>
            slot.Item?.Equals(item) == true && slot.Quantity < item.StackSize);

        if (stackableSlot is not null)
        {
            stackableSlot.Quantity++;
            _logger.Info($"Stacked item '{item.Name}' in existing slot. New quantity: {stackableSlot.Quantity}.");
            return true.ToResult();
        }

        var emptySlot = container.Inventory.FirstOrDefault(slot => slot.IsEmpty);
        if (emptySlot is not null)
        {
            emptySlot.Item = item;
            emptySlot.Quantity = 1;
            _logger.Info($"Placed item '{item.Name}' in empty slot.");
            return true.ToResult();
        }

        _logger.Warn($"Failed to add item '{item.Name}' — no free slot available.");
        return ErrorCodeDefinition.NoFreeSlot.ToFail<bool>("Brak wolnych slotów w ekwipunku.");
    }

    public ServiceResult<bool> RemoveItem(IInventoryContainer container, Item item)
    {
        _logger.Debug($"Attempting to remove item '{item.Name}' from inventory.");

        var slot = container.Inventory.FirstOrDefault(s => s.Item?.Equals(item) == true);
        if (slot is null)
        {
            _logger.Warn($"Item '{item.Name}' not found in inventory.");
            return ErrorCodeDefinition.ItemNotFound.ToFail<bool>("Nie znaleziono przedmiotu w ekwipunku.");
        }

        slot.Quantity--;
        _logger.Info($"Decreased quantity of item '{item.Name}' to {slot.Quantity}.");

        if (slot.Quantity <= 0)
        {
            slot.Item = null;
            slot.Quantity = 0;
            _logger.Info($"Item '{item.Name}' fully removed from slot.");
        }

        return true.ToResult();
    }

    public ServiceResult<bool> Contains(IInventoryContainer container, Item item)
    {
        var result = container.Inventory.Any(slot => slot.Item?.Equals(item) == true);
        _logger.Debug($"Checked if inventory contains item '{item.Name}': {result}");
        return result.ToResult();
    }

    public ServiceResult<bool> IsFull(IInventoryContainer container)
    {
        var result = container.Inventory.All(slot => !slot.IsEmpty && slot.Quantity >= slot.Item!.StackSize);
        _logger.Debug($"Checked if inventory is full: {result}");
        return result.ToResult();
    }

    public ServiceResult<int> FreeSpace(IInventoryContainer container)
    {
        var count = container.Inventory.Count(slot =>
            slot.IsEmpty || (slot.Item != null && slot.Quantity < slot.Item.StackSize));

        _logger.Debug($"Calculated free space in inventory: {count} slots available.");
        return count.ToResult();
    }
}