using RPG.Domain.Common;
using RPG.Domain.Common.Interfaces;
using RPG.Domain.Containers;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Skills;

namespace RPG.Domain.Models;

public sealed class Character : IDomainModel, IItemContainer, IStats, ILevel, ISkillsContainer
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

    // Player & Session
    public Guid PlayerId { get; set; }
    public Guid SessionId { get; set; }

    public CharacterClass Class { get; set; }

    // Containers (private)
    private InventoryContainer BankStorageContainer { get; }
    private InventoryContainer BackpackInventoryContainer { get; }
    private EquipmentContainer EquipmentContainer { get; }
    private StatsContainer BaseStatsContainer { get; }
    private StatsContainer ModifiedStatsContainer { get; }
    private SkillsContainer SkillsContainer { get; }

    // Public collections exposed from containers
    public IList<InventorySlot> BankStorage => BankStorageContainer.Inventory;
    public IList<InventorySlot> BackpackInventory => BackpackInventoryContainer.Inventory;
    public IDictionary<EquipmentSlot, Item> Equipments => EquipmentContainer.Equipments;
    public int Level { get; set; }
    public long Experience { get; set; }
    public long ExperienceToNextLevel { get; set; }
    public IDictionary<Skill, SkillAvailability> Skills => SkillsContainer.Skills;
    public IDictionary<Skill, DateTime> ActiveSkills => SkillsContainer.ActiveSkills;
    public IDictionary<StatsProperty, int> BaseStats => BaseStatsContainer.Stats;
    public IDictionary<StatsProperty, int> ModifiedStats => ModifiedStatsContainer.Stats;

    // Health & Resource
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentResource { get; set; }
    public int MaxResource { get; set; }
    public Location CurrentLocation { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsRotating { get; private set; }
    public bool IsOnline { get; set; }
    public bool IsInCombat { get; set; }
    public DateTime LastUpdated { get; set; }
    public HashSet<string> StatusEffects { get; set; }

    // Container accessors for services
    public IInventoryContainer GetBankStorageContainer()
    {
        return BankStorageContainer;
    }

    public IInventoryContainer GetBackpackInventoryContainer()
    {
        return BackpackInventoryContainer;
    }

    public IEquipmentContainer GetEquipmentContainer()
    {
        return EquipmentContainer;
    }

    public IStatsContainer GetBaseStatsContainer()
    {
        return BaseStatsContainer;
    }

    public IStatsContainer GetModifiedStatsContainer()
    {
        return ModifiedStatsContainer;
    }

    public ISkillsContainer GetSkillsContainer()
    {
        return SkillsContainer;
    }

    public void SetCurrentLocation(Location location)
    {
        CurrentLocation = location ?? throw new ArgumentNullException(nameof(location));
    }

    public void SetMovementState(bool isMoving)
    {
        IsMoving = isMoving;
    }

    public void SetRotationState(bool isRotating)
    {
        IsRotating = isRotating;
    }
}
