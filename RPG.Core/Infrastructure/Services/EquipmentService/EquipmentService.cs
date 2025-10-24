using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Domain.Interfaces;
using RPG.Core.Infrastructure.Services.InventoryService;
using RPG.Core.Interfaces;

namespace RPG.Core.Infrastructure.Services.EquipmentService;

public class EquipmentService : IEquipmentService
{
    private readonly IInventoryService _inventoryService;
    private readonly ISkillService _skillService;

    public EquipmentService(IInventoryService inventoryService, 
        ISkillService skillService)
    {
        _inventoryService = inventoryService;
        _skillService = skillService;
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