using RPG.Core.Common;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Enums;

namespace RPG.Core.Interfaces;

public interface IEquipmentService
{
    ServiceResult<bool> Equip(Character character, EquipmentSlot slot, Item item);
    ServiceResult<bool> Swap(Character character, EquipmentSlot slot, Item item);
    ServiceResult<bool> Unequip(Character character, EquipmentSlot slot);
    ServiceResult<bool> IsEquipped(Character character, EquipmentSlot slot);
    ServiceResult<Item[]> GetAllEquippedItems(Character character);
}
