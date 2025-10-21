using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Domain.Interfaces;

namespace RPG.Core.Infrastructure.Services.EquipmentService;

public class EquipmentService : IEquipmentService
{
    public bool Equip(IEquipment equipment, IInventory inventory, EquipmentSlot slot, Item item)
    {
        if (!inventory.InventoryItems.Contains(item)) return false;

        if (equipment.IsSlotFilled(slot))
            Unequip(equipment, inventory, slot);

        inventory.InventoryItems.Remove(item);
        equipment.EquipItem(slot, item);
        return true;
    }

    public bool Unequip(IEquipment equipment, IInventory inventory, EquipmentSlot slot)
    {
        if (inventory.InventoryItems.Count >= inventory.Capacity) return false;

        var item = equipment.GetEquippedItem(slot);
        if (item == null) return false;

        equipment.UnEquipItem(slot);
        inventory.InventoryItems.Add(item);
        return true;
    }

    public bool Swap(IEquipment equipment, IInventory inventory, EquipmentSlot slot, Item item)
    {
        if (!inventory.InventoryItems.Contains(item))
            return false;
        
        if (equipment.IsSlotFilled(slot))
        {
            var equippedItem = equipment.GetEquippedItem(slot);
            equipment.UnEquipItem(slot);
            inventory.InventoryItems.Remove(item);
            inventory.InventoryItems.Add(equippedItem);
            equipment.EquipItem(slot, item);
            return true;
        }
        else return Equip( equipment, inventory, slot, item);
    }

    public bool IsEquipped(IEquipment equipment, EquipmentSlot slot)
    {
        return equipment.IsSlotFilled(slot);
    }

    public IEnumerable<Item> GetAllEquippedItems(IEquipment equipment)
    {
        return equipment.GetAllEquippedItems();
    }
}