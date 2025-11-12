using System;
using System.Collections.Generic;
using RPG.Domain.Common;
using RPG.Domain.Containers;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;

namespace RPG.Domain.Entities.Npcs;

/// <summary>
///     Domain entity representing a Non-Player Character.
///     Uses tag-based and component-based system similar to Items.
///     Tags define what the NPC is (friendly, hostile, merchant, etc.)
///     Components define what the NPC can do (combat, dialogue, trading, etc.)
/// </summary>
public class Npc : IDomainModel
{
    public static Npc Create(
        string name,
        string displayName,
        Location spawnLocation,
        Guid worldId,
        HashSet<string>? tags = null)
    {
        return new Npc
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = displayName,
            Description = string.Empty,
            SpawnLocation = CloneLocation(spawnLocation),
            CurrentLocation = CloneLocation(spawnLocation),
            WorldId = worldId,
            Tags = tags ?? new HashSet<string>()
        };
    }

    private Npc()
    {
        Name = string.Empty;
        DisplayName = string.Empty;
        Description = string.Empty;
        SpawnLocation = new Location();
        CurrentLocation = new Location();
    Tags = new HashSet<string>();
    Components = new List<INpcComponent>();
    BaseStatsContainer = new StatsContainer();
    ModifiedStatsContainer = new StatsContainer();
    LastUpdated = DateTime.UtcNow;
    IsAlive = true;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string DisplayName { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Level { get; set; }
    public Location SpawnLocation { get; private set; }
    public Location CurrentLocation { get; private set; }
    public Guid WorldId { get; private set; }
    public HashSet<string> Tags { get; set; }
    public List<INpcComponent> Components { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public IDictionary<StatsProperty, int> BaseStats => BaseStatsContainer.Stats;
    public IDictionary<StatsProperty, int> ModifiedStats => ModifiedStatsContainer.Stats;
    public bool IsMoving { get; private set; }
    public bool IsRotating { get; private set; }
    public bool IsAlive { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime? RespawnAt { get; set; }

    private StatsContainer BaseStatsContainer { get; }
    private StatsContainer ModifiedStatsContainer { get; }

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
            Rotation = source.Rotation,
            MapId = source.MapId,
            ZoneName = source.ZoneName,
            WorldId = source.WorldId
        };

        return cloned;
    }
}
