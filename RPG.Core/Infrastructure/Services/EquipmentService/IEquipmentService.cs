using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Domain.Interfaces;

namespace RPG.Core.Infrastructure.Services.EquipmentService;

public interface IEquipmentService
{
    bool Equip(IEquipment equipment, IInventory inventory, EquipmentSlot slot, Item item);
    bool Swap(IEquipment equipment, IInventory inventory, EquipmentSlot slot, Item item);
    bool Unequip(IEquipment equipment, IInventory inventory, EquipmentSlot slot);
    bool IsEquipped(IEquipment equipment, EquipmentSlot slot);
    IEnumerable<Item> GetAllEquippedItems(IEquipment equipment);
}