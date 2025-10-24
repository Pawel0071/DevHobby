using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Domain.Interfaces;

namespace RPG.Core.Domain.Entities.Containers;

public class EquipmentContainer : IEquipmentContainer
{
    public EquipmentContainer() 
    {
        Equipments = Enum.GetValues(typeof(EquipmentSlot))
            .Cast<EquipmentSlot>()
            .ToDictionary(slot => slot, Item (slot) => null);
    }

    public Item this[EquipmentSlot slot]
    {
        get => Equipments[slot];
        set => Equipments[slot] = value;
    }

    public IDictionary<EquipmentSlot, Item> Equipments { get; }

    public bool IsEmpty(EquipmentSlot slot) => Equipments[slot] == null;
    
    public bool Contains(EquipmentSlot slot) => Equipments.ContainsKey(slot);
}