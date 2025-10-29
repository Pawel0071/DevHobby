using RPG.Core.Services.EquipmentService;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Enums;

namespace RPG.Core.Interfaces;

public interface IEquipmentService
{
    EquipmentResult Equip(Character character, EquipmentSlot slot, Item item);
    EquipmentResult Swap(Character character, EquipmentSlot slot, Item item);
    EquipmentResult Unequip(Character character, EquipmentSlot slot);
    bool IsEquipped(Character character, EquipmentSlot slot);
    IEnumerable<Item> GetAllEquippedItems(Character character);
}