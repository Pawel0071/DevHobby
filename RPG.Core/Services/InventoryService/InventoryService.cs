using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Logger;

namespace RPG.Core.Services.InventoryService;

public class InventoryService : IInventoryService
{
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(ILogger<InventoryService> logger)
    {
        _logger = logger;
    }

    public InventoryResult AddItem(IInventoryContainer container, Item item)
    {
        _logger.Debug($"Attempting to add item '{item.Name}' to inventory.");

        var stackableSlot = container.Inventory.FirstOrDefault(slot =>
            slot.Item?.Equals(item) == true && slot.Quantity < item.StackSize);

        if (stackableSlot is not null)
        {
            stackableSlot.Quantity++;
            _logger.Info($"Stacked item '{item.Name}' in existing slot. New quantity: {stackableSlot.Quantity}.");
            return InventoryResult.Ok();
        }

        var emptySlot = container.Inventory.FirstOrDefault(slot => slot.IsEmpty);
        if (emptySlot is not null)
        {
            emptySlot.Item = item;
            emptySlot.Quantity = 1;
            _logger.Info($"Placed item '{item.Name}' in empty slot.");
            return InventoryResult.Ok();
        }

        _logger.Warn($"Failed to add item '{item.Name}' — no free slot available.");
        return InventoryResult.Fail(InventoryError.NoFreeSlot, "Brak wolnych slotów w ekwipunku.");
    }

    public InventoryResult RemoveItem(IInventoryContainer container, Item item)
    {
        _logger.Debug($"Attempting to remove item '{item.Name}' from inventory.");

        var slot = container.Inventory.FirstOrDefault(s => s.Item?.Equals(item) == true);
        if (slot is null)
        {
            _logger.Warn($"Item '{item.Name}' not found in inventory.");
            return InventoryResult.Fail(InventoryError.ItemNotFound, "Nie znaleziono przedmiotu w ekwipunku.");
        }

        slot.Quantity--;
        _logger.Info($"Decreased quantity of item '{item.Name}' to {slot.Quantity}.");

        if (slot.Quantity <= 0)
        {
            slot.Item = null;
            slot.Quantity = 0;
            _logger.Info($"Item '{item.Name}' fully removed from slot.");
        }

        return InventoryResult.Ok();
    }

    public bool Contains(IInventoryContainer container, Item item)
    {
        var result = container.Inventory.Any(slot => slot.Item?.Equals(item) == true);
        _logger.Debug($"Checked if inventory contains item '{item.Name}': {result}");
        return result;
    }

    public bool IsFull(IInventoryContainer container)
    {
        var result = container.Inventory.All(slot => !slot.IsEmpty && slot.Quantity >= slot.Item!.StackSize);
        _logger.Debug($"Checked if inventory is full: {result}");
        return result;
    }

    public int FreeSpace(IInventoryContainer container)
    {
        var count = container.Inventory.Count(slot =>
            slot.IsEmpty || (slot.Item != null && slot.Quantity < slot.Item.StackSize));

        _logger.Debug($"Calculated free space in inventory: {count} slots available.");
        return count;
    }
}