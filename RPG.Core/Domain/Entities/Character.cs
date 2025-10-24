
using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Containers;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Domain.Interfaces;
using RPG.Core.Interfaces;


namespace RPG.Core.Domain.Entities;

public sealed class Character : IItemContainer, IStats, ILevel, ISkillsContainer
{
    public Character(
        Guid sessionId,
        CharacterClass characterClass)
    {
        Id = Guid.NewGuid();
        Class = characterClass;
        BaseStats = new StatsContainer();
        ModifiedStats = new StatsContainer();
        Equipments = new EquipmentContainer();
        BankStorage = new InventoryContainer(20);
        BackpackInventory = new InventoryContainer(20);
        Level = 1;
    }
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public CharacterClass Class { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int ExperienceToNextLevel { get; set; }
    public IInventoryContainer BankStorage { get; set; }
    public IInventoryContainer BackpackInventory { get; set; }
    public IEquipmentContainer Equipments { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    
    public int CurrentResource { get; set; }
    public int MaxResource { get; set; }
    public IStatsContainer BaseStats { get; set; }
    public IStatsContainer ModifiedStats { get; set; }
    
    public IDictionary<Skill, SkillAvailability> Skills { get; }
    
    public IDictionary<Skill, DateTime> ActiveSkills { get; }
    
    public ISession Session { get; set; }
    public IWorld World { get; set; }
    
    public bool IsOnline => World.Id != Guid.Empty;


}