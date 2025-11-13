using RPG.Domain.Common;
using RPG.Domain.Enums;
using RPG.Domain.Models.Items;

namespace RPG.Domain.Interfaces;

public interface IItemContainer
{
    public IList<InventorySlot> BankStorage { get; }
    public IList<InventorySlot> BackpackInventory { get; }
    public IDictionary<EquipmentSlot, Item> Equipments { get; }
}
