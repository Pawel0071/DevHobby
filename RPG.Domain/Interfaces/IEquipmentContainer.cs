using RPG.Domain.Entities.Items;
using RPG.Domain.Enums;

namespace RPG.Domain.Interfaces;

public interface IEquipmentContainer
{
    Item this[EquipmentSlot slot] { get; set; }
    IDictionary<EquipmentSlot, Item> Equipments { get; }
    bool IsEmpty(EquipmentSlot slot);
}
