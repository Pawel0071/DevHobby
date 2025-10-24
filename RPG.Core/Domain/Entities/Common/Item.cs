using RPG.Core.Domain.Entities.Containers;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Domain.Interfaces;

namespace RPG.Core.Domain.Entities.Common;

public class Item
{
    public Item()
    {
        Modifiers = new StatsContainer();
    }

    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public ItemType Type { get; set; }
    public IStatsContainer Modifiers { get; set; }
    public int RequiredLevel { get; set; }
    public int StackSize { get; set; }
    
    public bool CanEquip(bool dualWield, EquipmentSlot slot)
    {    
        return Type switch
        {
            ItemType.Amulet     => slot == EquipmentSlot.Amulet,
            ItemType.Head       => slot == EquipmentSlot.Head,
            ItemType.Feet       => slot == EquipmentSlot.Feet,
            ItemType.Legs       => slot == EquipmentSlot.Legs,
            ItemType.Hands      => slot == EquipmentSlot.Hands,
            ItemType.Waist      => slot == EquipmentSlot.Waist,
            ItemType.Ring       => slot == EquipmentSlot.Ring1 || slot == EquipmentSlot.Ring2,
            ItemType.Weapon1H   => slot == EquipmentSlot.Weapon1 || slot == EquipmentSlot.Weapon2,
            ItemType.Weapon2H   => slot == EquipmentSlot.Weapon1,
            ItemType.Offhand    => slot == EquipmentSlot.Weapon2,
            _ => false
        };
    }

    public bool CanUse()
    {
        return Type == ItemType.Consumable;
    }
}