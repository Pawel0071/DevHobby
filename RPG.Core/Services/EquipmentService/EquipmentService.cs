using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Enums;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Logger;

namespace RPG.Core.Services.EquipmentService;

public class EquipmentService : IEquipmentService
{
    private readonly IInventoryService _inventoryService;
    private readonly ISkillService _skillService;
    private readonly ILogger<EquipmentService> _logger;

    public EquipmentService(
        IInventoryService inventoryService, 
        ISkillService skillService,
        ILogger<EquipmentService> logger)
    {
        _inventoryService = inventoryService;
        _skillService = skillService;
        _logger = logger;
    }

    public EquipmentResult Equip(Character character, EquipmentSlot slot, Item item)
    {
        _logger.Debug($"Attempting to equip item '{item.Name}' to slot '{slot}' for character '{character.Id}'.");

        if (!_inventoryService.Contains(character.BackpackInventory, item))
        {
            _logger.Warn($"Item '{item.Name}' not found in inventory. Cannot equip.");
            return EquipmentResult.Fail(EquipmentError.ItemCannotBeEquip, "Przedmiot nie znajduje się w ekwipunku.");
        }

        var currentlyEquipped = character.Equipments[slot];
        if (currentlyEquipped != null)
        {
            _logger.Debug($"Slot '{slot}' already contains item '{currentlyEquipped.Name}'. Attempting to unequip.");
            var unequipResult = Unequip(character, slot);
            if (!unequipResult.Success)
            {
                _logger.Error($"Failed to unequip item from slot '{slot}' for character '{character.Id}'.");
                return unequipResult;
            }
        }

        var removeResult = _inventoryService.RemoveItem(character.BackpackInventory, item);
        if (!removeResult.Success)
        {
            _logger.Error($"Failed to remove item '{item.Name}' from inventory: {removeResult.Message}");
            return EquipmentResult.Fail(EquipmentError.InvalidOperation, $"Nie udało się usunąć przedmiotu z ekwipunku: {removeResult.Message}");
        }

        character.Equipments[slot] = item;
        _logger.Info($"Equipped item '{item.Name}' to slot '{slot}' for character '{character.Id}'.");
        return EquipmentResult.Ok();
    }

    public EquipmentResult Unequip(Character character, EquipmentSlot slot)
    {
        var item = character.Equipments[slot];
        if (item == null)
        {
            _logger.Warn($"Attempted to unequip from empty slot '{slot}' for character '{character.Id}'.");
            return EquipmentResult.Fail(EquipmentError.InvalidOperation, "Slot jest pusty.");
        }

        var addResult = _inventoryService.AddItem(character.BackpackInventory, item);
        if (!addResult.Success)
        {
            _logger.Error($"Failed to add item '{item.Name}' back to inventory: {addResult.Message}");
            return EquipmentResult.Fail(EquipmentError.InvalidOperation, $"Nie udało się dodać przedmiotu do ekwipunku: {addResult.Message}");
        }

        character.Equipments[slot] = null!;
        _logger.Info($"Unequipped item '{item.Name}' from slot '{slot}' for character '{character.Id}'.");
        return EquipmentResult.Ok();
    }

    public EquipmentResult Swap(Character character, EquipmentSlot slot, Item item)
    {
        _logger.Debug($"Swapping item '{item.Name}' into slot '{slot}' for character '{character.Id}'.");

        if (!_inventoryService.Contains(character.BackpackInventory, item))
        {
            _logger.Warn($"Item '{item.Name}' not found in inventory. Cannot swap.");
            return EquipmentResult.Fail(EquipmentError.ItemCannotBeEquip, "Przedmiot nie znajduje się w ekwipunku.");
        }

        var equippedItem = character.Equipments[slot];
        if (equippedItem == null) return Equip(character, slot, item);
        var unequipResult = Unequip(character, slot);
        return !unequipResult.Success ? unequipResult : Equip(character, slot, item);
    }

    public bool IsEquipped(Character character, EquipmentSlot slot)
    {
        var equipped = character.Equipments[slot] != null;
        _logger.Debug($"Checked if slot '{slot}' is equipped for character '{character.Id}': {equipped}");
        return equipped;
    }

    public IEnumerable<Item> GetAllEquippedItems(Character character)
    {
        var equippedItems = character.Equipments.Equipments.Values.Where(item => item != null)!;
        var allEquippedItems = equippedItems as Item[] ?? equippedItems.ToArray();
        _logger.Debug($"Retrieved all equipped items for character '{character.Id}'. Count: {allEquippedItems.Length}");
        return allEquippedItems;
    }
}