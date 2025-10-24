using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;

namespace RPG.Core.Domain.Interfaces;

public interface IEquipmentContainer
{
    Item this[EquipmentSlot slot] { get; set; }
    IDictionary<EquipmentSlot, Item> Equipments { get; }
    bool IsEmpty(EquipmentSlot slot);
}