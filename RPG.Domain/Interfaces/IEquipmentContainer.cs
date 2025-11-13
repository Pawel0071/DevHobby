using RPG.Domain.Enums;
using RPG.Domain.Models.Items;

namespace RPG.Domain.Interfaces;

public interface IEquipmentContainer
{
    Item this[EquipmentSlot slot] { get; set; }
    IDictionary<EquipmentSlot, Item> Equipments { get; }
    bool IsEmpty(EquipmentSlot slot);
}
