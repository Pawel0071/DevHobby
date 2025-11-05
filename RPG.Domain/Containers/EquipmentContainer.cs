using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;

namespace RPG.Domain.Containers;

public class EquipmentContainer : IEquipmentContainer
{
    public EquipmentContainer() 
    {
        Equipments = Enum.GetValues(typeof(EquipmentSlot))
            .Cast<EquipmentSlot>()
            .ToDictionary(slot => slot, Item (slot) => null!);
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