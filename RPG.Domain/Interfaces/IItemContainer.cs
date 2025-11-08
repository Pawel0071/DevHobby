using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Domain.Enums;

namespace RPG.Domain.Interfaces;

public interface IItemContainer
{
    public IList<InventorySlot> BankStorage { get; }
    public IList<InventorySlot> BackpackInventory { get; }
    public IDictionary<EquipmentSlot, Item> Equipments { get; }
}
