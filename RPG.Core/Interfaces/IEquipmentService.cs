using RPG.Core.Common;
using RPG.Domain.Enums;
using RPG.Domain.Models;
using RPG.Domain.Models.Items;

namespace RPG.Core.Interfaces;

public interface IEquipmentService
{
    ServiceResult<bool> Equip(Character character, EquipmentSlot slot, Item item);
    ServiceResult<bool> Swap(Character character, EquipmentSlot slot, Item item);
    ServiceResult<bool> Unequip(Character character, EquipmentSlot slot);
    ServiceResult<bool> IsEquipped(Character character, EquipmentSlot slot);
    ServiceResult<Item[]> GetAllEquippedItems(Character character);
}
