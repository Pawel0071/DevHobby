using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Domain.Interfaces;

namespace RPG.Core.Infrastructure.Services.EquipmentService;

public interface IEquipmentService
{
    EquipmentResult Equip(Character character, EquipmentSlot slot, Item item);
    EquipmentResult Swap(Character character, EquipmentSlot slot, Item item);
    EquipmentResult Unequip(Character character, EquipmentSlot slot);
    bool IsEquipped(Character character, EquipmentSlot slot);
    IEnumerable<Item> GetAllEquippedItems(Character character);
}