namespace RPG.Core.Domain.Interfaces;

public interface IItemContainer
{
    public IInventory BankStorage { get; } 
    public IInventory BackpackInventory { get; } 
    public IEquipment Equipment { get; } 
}