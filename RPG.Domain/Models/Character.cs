using RPG.Domain.Common;
using RPG.Domain.Common.Interfaces;
using RPG.Domain.Containers;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Skills;

namespace RPG.Domain.Models;

public sealed class Character : IDomainModel,
    IItemContainer,
    ILevel,
    ISkillAndCombat,
    IMovable,
    ISession
{
    public Character(
        Guid sessionId,
        CharacterClass characterClass,
        object? session = null,
        object? world = null)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        Class = characterClass;
        SkillsContainer = new SkillsContainer();
        BaseStatsContainer = new StatsContainer();
        ModifiedStatsContainer = new StatsContainer();
        EquipmentContainer = new EquipmentContainer();
        BankStorageContainer = new InventoryContainer(20);
        BackpackInventoryContainer = new InventoryContainer(20);
        CurrentLocation = new Location();
        Level = 1;
        StatusEffects = new HashSet<string>();
        LastUpdated = DateTime.UtcNow;
    }

    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required CharacterClass Class { get; init; }
    public HashSet<string> StatusEffects { get; set; }

    // IMovable
    public Location SpawnLocation { get; set; }
    public Location CurrentLocation { get; set; }
    public Guid? WorldId => CurrentLocation.WorldId;
    public bool IsMoving { get; set; }
    public bool IsRotating { get; set; }

    //ISession
    public Guid PlayerId { get; init; }
    public Guid SessionId { get; init; }
    public bool IsOnline { get; set; }
    public DateTime LastUpdated { get; set; }

    // IItemContainer
    private InventoryContainer BankStorageContainer { get; }
    private InventoryContainer BackpackInventoryContainer { get; }
    private EquipmentContainer EquipmentContainer { get; }
    public IList<InventorySlot> BankStorage => BankStorageContainer.Inventory;
    public IList<InventorySlot> BackpackInventory => BackpackInventoryContainer.Inventory;
    public IDictionary<EquipmentSlot, Item> Equipments => EquipmentContainer.Equipments;

    // IStats
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentResource { get; set; }
    public int MaxResource { get; set; }
    private StatsContainer BaseStatsContainer { get; }
    private StatsContainer ModifiedStatsContainer { get; }
    public IDictionary<StatsProperty, int> BaseStats => BaseStatsContainer.Stats;
    public IDictionary<StatsProperty, int> ModifiedStats => ModifiedStatsContainer.Stats;

    // ILevel
    public int Level { get; set; }
    public long Experience { get; set; }
    public long ExperienceToNextLevel { get; set; }

    // ISkills
    private SkillsContainer SkillsContainer { get; }
    public IDictionary<Skill, SkillAvailability> Skills => SkillsContainer.Skills;
    public IDictionary<Skill, DateTime> ActiveSkills => SkillsContainer.ActiveSkills;

    // ISkillAndCombat & ICombatTarget
    public bool IsAlive => CurrentHealth > 0;
    public bool IsInCombat { get; set; }
}

