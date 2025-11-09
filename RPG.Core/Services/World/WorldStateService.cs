using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Core.Interfaces;
using RPG.Domain.Entities;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.MapObjects.MapObjectComponents;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Entities.Npcs.NpcComponents;

namespace RPG.Core.Services.World;

public class WorldStateService : IWorldStateService
{
    public void UpsertCharacter(WorldState world, Character character)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(character);

        var copy = CloneCharacter(character);
        var index = world.Characters.FindIndex(c => c.Id == copy.Id);
        if (index >= 0)
        {
            world.Characters[index] = copy;
        }
        else
        {
            world.Characters.Add(copy);
        }

        TouchFrom(world, copy.LastUpdated);
    }

    public void RemoveCharacter(WorldState world, Guid characterId)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Characters.RemoveAll(c => c.Id == characterId) > 0)
        {
            Touch(world, DateTime.UtcNow);
        }
    }

    public void UpsertNpc(WorldState world, Npc npc)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(npc);

        var copy = CloneNpc(npc);
        var index = world.Npcs.FindIndex(n => n.Id == copy.Id);
        if (index >= 0)
        {
            world.Npcs[index] = copy;
        }
        else
        {
            world.Npcs.Add(copy);
        }

        TouchFrom(world, copy.LastUpdated);
    }

    public void UpsertMapObject(WorldState world, MapObject mapObject)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(mapObject);

        var copy = CloneMapObject(mapObject);
        var index = world.MapObjects.FindIndex(o => o.Id == copy.Id);
        if (index >= 0)
        {
            world.MapObjects[index] = copy;
        }
        else
        {
            world.MapObjects.Add(copy);
        }

        TouchFrom(world, copy.LastUpdated);
    }

    public void Touch(WorldState world, DateTime timestamp)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.LastUpdated = timestamp;
    }

    public WorldState Clone(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        return WorldState.Hydrate(
            world.Id,
            world.WorldId,
            world.WorldName,
            world.LastUpdated,
            world.Characters.Select(CloneCharacter),
            world.Npcs.Select(CloneNpc),
            world.MapObjects.Select(CloneMapObject));
    }

    private static void TouchFrom(WorldState world, DateTime timestamp)
    {
        var effective = timestamp == default ? DateTime.UtcNow : timestamp;
        world.LastUpdated = effective;
    }

    private static Character CloneCharacter(Character source)
    {
        var clone = new Character(source.SessionId, source.Class)
        {
            Id = source.Id,
            Name = source.Name,
            PlayerId = source.PlayerId,
            Level = source.Level,
            Experience = source.Experience,
            ExperienceToNextLevel = source.ExperienceToNextLevel,
            CurrentHealth = source.CurrentHealth,
            MaxHealth = source.MaxHealth,
            CurrentResource = source.CurrentResource,
            MaxResource = source.MaxResource,
            IsOnline = source.IsOnline,
            IsInCombat = source.IsInCombat,
            LastUpdated = source.LastUpdated,
            StatusEffects = new HashSet<string>(source.StatusEffects)
        };

        clone.SetCurrentLocation(CloneLocation(source.CurrentLocation));
        clone.SetMovementState(source.IsMoving);
        clone.SetRotationState(source.IsRotating);

        foreach (var stat in source.BaseStats)
        {
            clone.BaseStats[stat.Key] = stat.Value;
        }

        foreach (var stat in source.ModifiedStats)
        {
            clone.ModifiedStats[stat.Key] = stat.Value;
        }

        return clone;
    }

    private static Npc CloneNpc(Npc source)
    {
        var clone = Npc.Create(source.Name, source.DisplayName, CloneLocation(source.SpawnLocation), source.WorldId, new HashSet<string>(source.Tags));

        typeof(Npc).GetProperty("Id")!.SetValue(clone, source.Id);
        clone.Description = source.Description;
        clone.Level = source.Level;
        clone.CurrentHealth = source.CurrentHealth;
        clone.MaxHealth = source.MaxHealth;
        clone.SetCurrentLocation(CloneLocation(source.CurrentLocation));
        clone.SetMovementState(source.IsMoving);
        clone.SetRotationState(source.IsRotating);
        clone.IsAlive = source.IsAlive;
        clone.LastUpdated = source.LastUpdated;
        clone.RespawnAt = source.RespawnAt;

        foreach (var stat in source.BaseStats)
        {
            clone.BaseStats[stat.Key] = stat.Value;
        }

        foreach (var stat in source.ModifiedStats)
        {
            clone.ModifiedStats[stat.Key] = stat.Value;
        }

        clone.Components = source.Components is null
            ? new List<INpcComponent>()
            : new List<INpcComponent>(source.Components);

        return clone;
    }

    private static MapObject CloneMapObject(MapObject source)
    {
        var locationClone = CloneLocation(source.Location);
        var clone = MapObject.Create(source.Name, locationClone, source.WorldId, source.ZoneId);

        typeof(MapObject).GetProperty("Id")!.SetValue(clone, source.Id);
        clone.DisplayName = source.DisplayName;
        clone.Description = source.Description;
        clone.IsActive = source.IsActive;
        clone.RotationYaw = source.RotationYaw;
        clone.LastUpdated = source.LastUpdated;
        clone.Tags = new HashSet<string>(source.Tags);
        clone.State = new Dictionary<string, string>(source.State);
        clone.Components = source.Components is null
            ? new List<IMapObjectComponent>()
            : new List<IMapObjectComponent>(source.Components);

        return clone;
    }

    private static Location CloneLocation(Location location)
    {
        var clone = new Location
        {
            WorldId = location.WorldId,
            MapId = location.MapId,
            ZoneName = location.ZoneName,
            Rotation = location.Rotation
        };

        clone.Position = location.Position;
        return clone;
    }
}
