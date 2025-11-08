using System.Linq;
using RPG.Core.Common;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Domain.Enums;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.EquipmentService;

public class EquipmentService : IEquipmentService
{
    private const string EquippableTag = "item:equippable";

    private readonly IInventoryService _inventoryService;
    private readonly ILogger<EquipmentService> _logger;
    private readonly ISkillService _skillService;

    public EquipmentService(
        IInventoryService inventoryService,
        ISkillService skillService,
        ILogger<EquipmentService> logger)
    {
        _inventoryService = inventoryService;
        _skillService = skillService;
        _logger = logger;
    }

    public ServiceResult<bool> Equip(Character character, EquipmentSlot slot, Item item)
    {
        _logger.Debug($"Attempting to equip item '{item.Name}' to slot '{slot}' for character '{character.Id}'.");

        var backpack = character.GetBackpackInventoryContainer();

        if (!_inventoryService.Contains(backpack, item).Result)
        {
            _logger.Warn($"Item '{item.Name}' not found in inventory. Cannot equip.");
            return ErrorCodeDefinition.ItemNotFound.ToFail<bool>("Przedmiot nie znajduje się w ekwipunku.");
        }

        if (!item.Tags.Contains(EquippableTag))
        {
            _logger.Warn($"Item '{item.Name}' is missing required tag '{EquippableTag}'.");
            return ErrorCodeDefinition.ItemNotEquippable.ToFail<bool>("Tego przedmiotu nie można założyć.");
        }

        var equippableComponent = item.GetComponent<EquippableComponent>();
        if (equippableComponent is null)
        {
            _logger.Warn($"Item '{item.Name}' does not define equippable metadata.");
            return ErrorCodeDefinition.EquipmentMetadataMissing.ToFail<bool>("Tego przedmiotu nie można założyć.");
        }

        if (equippableComponent.ValidSlots is null || equippableComponent.ValidSlots.Count == 0)
        {
            _logger.Warn($"Item '{item.Name}' does not declare valid equipment slots.");
            return ErrorCodeDefinition.EquipmentMetadataMissing.ToFail<bool>("Brak zgodnych slotów dla tego przedmiotu.");
        }

        if (!equippableComponent.ValidSlots.Contains(slot))
        {
            _logger.Warn($"Slot '{slot}' is not valid for item '{item.Name}'.");
            return ErrorCodeDefinition.EquipmentSlotMismatch.ToFail<bool>("Przedmiot nie pasuje do wybranego slotu.");
        }

        if (equippableComponent.IsUniqueEquip)
        {
            var alreadyEquipped = character.Equipments
                .Where(kvp => kvp.Value != null && kvp.Key != slot)
                .Select(kvp => kvp.Value)
                .Any(equipped => equipped != null && equipped.TypeCode == item.TypeCode);

            if (alreadyEquipped)
            {
                _logger.Warn(
                    $"Unique equip restriction: another instance of type '{item.TypeCode}' is already equipped.");
                return ErrorCodeDefinition.UniqueEquipViolation.ToFail<bool>(
                    "Ten przedmiot może być założony tylko raz.");
            }
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

        var removeResult = _inventoryService.RemoveItem(backpack, item);
        if (!removeResult.Success)
        {
            _logger.Error($"Failed to remove item '{item.Name}' from inventory: {removeResult.Message}");
            return ErrorCodeDefinition.InvalidOperation.ToFail<bool>(
                $"Nie udało się usunąć przedmiotu z ekwipunku: {removeResult.Message}");
        }

        character.Equipments[slot] = item;
        _logger.Info($"Equipped item '{item.Name}' to slot '{slot}' for character '{character.Id}'.");
        return true.ToResult();
    }

    public ServiceResult<bool> Unequip(Character character, EquipmentSlot slot)
    {
        var item = character.Equipments[slot];
        if (item == null)
        {
            _logger.Warn($"Attempted to unequip from empty slot '{slot}' for character '{character.Id}'.");
            return ErrorCodeDefinition.InvalidOperation.ToFail<bool>("Slot jest pusty.");
        }

        var addResult = _inventoryService.AddItem(character.GetBackpackInventoryContainer(), item);
        if (!addResult.Success)
        {
            _logger.Error($"Failed to add item '{item.Name}' back to inventory: {addResult.Message}");
            return ErrorCodeDefinition.InvalidOperation.ToFail<bool>(
                $"Nie udało się dodać przedmiotu do ekwipunku: {addResult.Message}");
        }

        character.Equipments[slot] = null!;
        _logger.Info($"Unequipped item '{item.Name}' from slot '{slot}' for character '{character.Id}'.");
        return true.ToResult();
    }

    public ServiceResult<bool> Swap(Character character, EquipmentSlot slot, Item item)
    {
        _logger.Debug($"Swapping item '{item.Name}' into slot '{slot}' for character '{character.Id}'.");

        if (!_inventoryService.Contains(character.GetBackpackInventoryContainer(), item).Result)
        {
            _logger.Warn($"Item '{item.Name}' not found in inventory. Cannot swap.");
            return ErrorCodeDefinition.ItemNotFound.ToFail<bool>("Przedmiot nie znajduje się w ekwipunku.");
        }

        var equippedItem = character.Equipments[slot];
        if (equippedItem == null) return Equip(character, slot, item);

        var unequipResult = Unequip(character, slot);
        return !unequipResult.Success ? unequipResult : Equip(character, slot, item);
    }

    public ServiceResult<bool> IsEquipped(Character character, EquipmentSlot slot)
    {
        var equipped = character.Equipments[slot] != null;
        _logger.Debug($"Checked if slot '{slot}' is equipped for character '{character.Id}': {equipped}");
        return equipped.ToResult();
    }

    public ServiceResult<Item[]> GetAllEquippedItems(Character character)
    {
        var equippedItems = character.Equipments.Values.Where(item => item != null)!;
        var allEquippedItems = equippedItems as Item[] ?? equippedItems.ToArray();
        _logger.Debug($"Retrieved all equipped items for character '{character.Id}'. Count: {allEquippedItems.Length}");
        return allEquippedItems.ToResult();
    }
}
