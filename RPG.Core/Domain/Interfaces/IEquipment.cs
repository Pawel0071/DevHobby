using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;

namespace RPG.Core.Domain.Interfaces;

public interface IEquipment
{
    bool IsInInventory(Item item);
    Dictionary<EquipmentSlot, Item> EquipmentItems { get; set; }
    Item GetEquippedItem(EquipmentSlot slot);
    void EquipItem(EquipmentSlot slot, Item item);
    void UnEquipItem(EquipmentSlot slot);
    bool IsSlotFilled(EquipmentSlot slot);
    IList<Item> GetAllEquippedItems();
}