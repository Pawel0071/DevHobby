
using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Interfaces;
using RPG.Core.Interfaces;


namespace RPG.Core.Domain.Entities;

public sealed class PlayerCharacter : IItemContainer
{
    public PlayerCharacter(
        Guid sessionId,
        CharacterClass characterClass)
    {
        Id = Guid.NewGuid();
        Class = characterClass;
        BankStorage = new Inventory(20);
        BackpackInventory = new Inventory(20);
        Level = 1;
    }
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public ILevel Level { get; set; }
    public IClass Class { get; set; }

    public IInventory BankStorage { get; }
    
    public IInventory BackpackInventory { get; }
    public IEquipment Equipment { get; }
    public IStats BaseStats { get; set; }
    
    public IStats ModifiedStats { get; set; }
    
    public ISkills BaseSkills { get; set; }
    
    public ISkills ModifiedSkills { get; set; }
    
    public IBuffs Buffs { get; set; }
    
    public ISession Session { get; set; }
    public IWorld World { get; set; }
    
    public bool IsOnline => World.Id != Guid.Empty;
}
 