using RPG.Core.Interfaces;
using RPG.Core.Services.InventoryService;
using RPG.Core.Services.SkillService;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Enums;
using RPG.Infrastructure.Logger;

namespace RPG.Core.Services.EquipmentService;

public class EquipmentService : IEquipmentService
{
    private readonly IInventoryService _inventoryService;
    private readonly ISkillService _skillService;
    private readonly ILogger<EquipmentService> _logger;

    public EquipmentService(IInventoryService inventoryService, 
        ISkillService skillService,
        ILogger<EquipmentService> logger)
    {
        _inventoryService = inventoryService;
        _skillService = skillService;
        _logger = logger;
    }

    public EquipmentResult Equip(Character character, EquipmentSlot slot, Item item)
    {
        if (!_inventoryService.Contains(character.BackpackInventory, item))
            return EquipmentResult.Fail(EquipmentError.ItemCannotBeEquip, "Przedmiot nie znajduje się w ekwipunku.");

        var currentlyEquipped = character.Equipments[slot];
        if (currentlyEquipped != null)
        {
            var unequipResult = Unequip(character, slot);
            if (!unequipResult.Success)
                return unequipResult;
        }

        var removeResult = _inventoryService.RemoveItem(character.BackpackInventory, item);
        if (!removeResult.Success)
            return EquipmentResult.Fail(EquipmentError.InvalidOperation, $"Nie udało się usunąć przedmiotu z ekwipunku: {removeResult.Message}");

        character.Equipments[slot] = item;
        return EquipmentResult.Ok();
    }

    public EquipmentResult Unequip(Character character, EquipmentSlot slot)
    {
        var item = character.Equipments[slot];
        if (item == null)
            return EquipmentResult.Fail(EquipmentError.InvalidOperation, "Slot jest pusty.");

        var addResult = _inventoryService.AddItem(character.BackpackInventory, item);
        if (!addResult.Success)
            return EquipmentResult.Fail(EquipmentError.InvalidOperation, $"Nie udało się dodać przedmiotu do ekwipunku: {addResult.Message}");

        character.Equipments[slot] = null!;
        return EquipmentResult.Ok();
    }

    public EquipmentResult Swap(Character character, EquipmentSlot slot, Item item)
    {
        if (!_inventoryService.Contains(character.BackpackInventory, item))
            return EquipmentResult.Fail(EquipmentError.ItemCannotBeEquip, "Przedmiot nie znajduje się w ekwipunku.");

        var equippedItem = character.Equipments[slot];
        if (equippedItem != null)
        {
            var unequipResult = Unequip(character, slot);
            if (!unequipResult.Success)
                return unequipResult;
        }

        return Equip(character, slot, item);
    }

    public bool IsEquipped(Character character, EquipmentSlot slot) =>
        character.Equipments[slot] != null;

    public IEnumerable<Item> GetAllEquippedItems(Character character) =>
        character.Equipments.Equipments.Values.Where(item => item != null)!;
}