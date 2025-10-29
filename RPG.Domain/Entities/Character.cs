using RPG.Domain.Common;
using RPG.Domain.Common.Interfaces;
using RPG.Domain.Containers;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;

namespace RPG.Domain.Entities;

public sealed class Character : IItemContainer, IStats, ILevel, ISkillsContainer
{
    public Character(
        Guid sessionId,
        CharacterClass characterClass, ISession session, IWorld world)
    {
        Id = Guid.NewGuid();
        Class = characterClass;
        Session = session;
        World = world;
        Skills = new Dictionary<Skill, SkillAvailability>();
        ActiveSkills = new Dictionary<Skill, DateTime>();
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

public interface IWorld
{
    Guid Id { get; set; }
}

public interface ISession
{
}