using RPG.Domain.Common;
using RPG.Domain.Common.Interfaces;
using RPG.Domain.Containers;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Domain.Models.Skills;

namespace RPG.Domain.Models.Npcs;

/// <summary>
///     Domain entity representing a Non-Player Character.
///     Uses tag-based and component-based system similar to Items.
///     Tags define what the NPC is (friendly, hostile, merchant, etc.)
///     Components define what the NPC can do (combat, dialogue, trading, etc.)
/// </summary>
public class Npc : IDomainModel,
    IMovable,
    ISkillAndCombat
{
    public Npc()
    {
        Id = Guid.NewGuid();
        SkillsContainer = new SkillsContainer();
        BaseStatsContainer = new StatsContainer();
        ModifiedStatsContainer = new StatsContainer();
        CurrentLocation = new Location();
        LastUpdated = DateTime.UtcNow;
    }

    public static Npc Create(
        string name,
        string displayName,
        Location spawnLocation,
        Guid worldId,
        HashSet<string>? tags = null)
    {
        spawnLocation.WorldId = worldId;
        return new Npc()
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = displayName,
            Description = string.Empty,
            SpawnLocation = CloneLocation(spawnLocation),
            CurrentLocation = CloneLocation(spawnLocation),
            Tags = tags ?? new HashSet<string>()
        };
    }

    public Guid Id { get; init; }
    public string Name { get; init; }
    public HashSet<string> Tags { get; set; } = [];
    public List<INpcComponent> Components { get; set; } = [];

    public int Level { get; set; }

    // IStats
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentResource { get; set; }
    public int MaxResource { get; set; }
    private StatsContainer BaseStatsContainer { get; }
    private StatsContainer ModifiedStatsContainer { get; }
    public IDictionary<StatsProperty, int> BaseStats => BaseStatsContainer.Stats;
    public IDictionary<StatsProperty, int> ModifiedStats => ModifiedStatsContainer.Stats;

    // ISkills
    private SkillsContainer SkillsContainer { get; }
    public IDictionary<Skill, SkillAvailability> Skills => SkillsContainer.Skills;
    public IDictionary<Skill, DateTime> ActiveSkills => SkillsContainer.ActiveSkills;

    // ISkillAndCombat & ICombatTarget
    public bool IsAlive => CurrentHealth > 0;
    public bool IsInCombat { get; set; }

    // IMovable
    public Location SpawnLocation { get; set; }
    public Location CurrentLocation { get; set; }
    public Guid WorldId => CurrentLocation.WorldId;
    public bool IsMoving { get; set; }
    public bool IsRotating { get; set; }


    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;


    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime? RespawnAt { get; set; }


    public IStatsContainer GetBaseStatsContainer()
    {
        return BaseStatsContainer;
    }

    public IStatsContainer GetModifiedStatsContainer()
    {
        return ModifiedStatsContainer;
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

    private static Location CloneLocation(Location source)
    {
        if (source == null)
        {
            return new Location();
        }

        var cloned = new Location
        {
            Position = source.Position,
            Direction = source.Direction,
            MapId = source.MapId,
            MapName = source.MapName,
            WorldId = source.WorldId
        };

        return cloned;
    }
}


