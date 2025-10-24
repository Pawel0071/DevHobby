namespace RPG.Core.Domain.Interfaces;

public interface IItemContainer
{
    public IInventoryContainer BankStorage { get; set; }
    public IInventoryContainer BackpackInventory { get; set; }
    public IEquipmentContainer Equipments { get; set; }
}