using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using RPG.Core.Interfaces;
using RPG.Domain.Models;
using RPG.Domain.Models.MapObjects;
using RPG.Domain.Models.Npcs;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.World;

public class WorldStateService : IWorldStateService
{
    private const string DefaultMapId = "starter.map";
    private const string DefaultZoneName = "starter.zone";
    private const string DefaultSpawnType = "player-default";
    private const string SpawnPointTag = "spawn-point";
    private const string SpawnTypeStateKey = "spawnType";
    private const string SpawnPriorityStateKey = "priority";
    private const string SpawnRotationStateKey = "rotation";
    private const string FriendlyTag = "friendly";
    private const string GuideTag = "guide";

    private readonly IModelRepository _modelRepository;

    public WorldStateService(IModelRepository modelRepository)
    {
        _modelRepository = modelRepository;
    }

    public void UpsertCharacter(WorldState world, Character character)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(character);

        if (!world.Characters.Contains(character.Id))
        {
            world.Characters.Add(character.Id);
        }

        TouchFrom(world, character.LastUpdated);
    }

    public void RemoveCharacter(WorldState world, Guid characterId)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Characters.Remove(characterId))
        {
            Touch(world, DateTime.UtcNow);
        }
    }

    public void UpsertNpc(WorldState world, Npc npc)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(npc);

        if (!world.Npcs.Contains(npc.Id))
        {
            world.Npcs.Add(npc.Id);
        }

        TouchFrom(world, npc.LastUpdated);
    }

    public void UpsertMapObject(WorldState world, MapObject mapObject)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(mapObject);

        if (!world.MapObjects.Contains(mapObject.Id))
        {
            world.MapObjects.Add(mapObject.Id);
        }

        TouchFrom(world, mapObject.LastUpdated);
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
            world.Characters.ToList(),
            world.Npcs.ToList(),
            world.MapObjects.ToList());
    }

    public async Task<Location> DetermineSpawnLocationAsync(
        WorldState world,
        Character character,
        string? spawnType = null,
        bool useExistingLocation = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(character);

        if (useExistingLocation && character.CurrentLocation != null && character.CurrentLocation.WorldId == world.WorldId)
        {
            var existing = CloneLocation(character.CurrentLocation);
            EnsureDefaults(existing, world.WorldId);
            return existing;
        }

        var requestedSpawnType = string.IsNullOrWhiteSpace(spawnType) ? DefaultSpawnType : spawnType;

        MapObject? bestSpawn = null;
        var bestPriority = int.MaxValue;

        foreach (var mapObjectId in world.MapObjects)
        {
            var mapObject = await _modelRepository.GetByIdAsync<MapObject>(mapObjectId, cancellationToken).ConfigureAwait(false);
            if (mapObject is null)
            {
                continue;
            }

            if (!MatchesSpawnType(mapObject, requestedSpawnType))
            {
                continue;
            }

            var priority = ParsePriority(mapObject.State);
            if (priority < bestPriority)
            {
                bestPriority = priority;
                bestSpawn = mapObject;
            }
        }

        if (bestSpawn != null)
        {
            var location = CloneLocation(bestSpawn.Location);

            if (bestSpawn.State.TryGetValue(SpawnRotationStateKey, out var rotationText) &&
                float.TryParse(rotationText, NumberStyles.Float, CultureInfo.InvariantCulture, out var rotationValue))
            {
                location.Direction = rotationValue;
            }
            else if (bestSpawn.Location != null)
            {
                location.Direction = bestSpawn.Location.Direction;
            }

            EnsureDefaults(location, world.WorldId);
            return location;
        }

        foreach (var npcId in world.Npcs)
        {
            var npc = await _modelRepository.GetByIdAsync<Npc>(npcId, cancellationToken).ConfigureAwait(false);
            if (npc is null)
            {
                continue;
            }

            var hasGuideTag = npc.Tags.Any(tag => string.Equals(tag, GuideTag, StringComparison.OrdinalIgnoreCase));
            var hasFriendlyTag = npc.Tags.Any(tag => string.Equals(tag, FriendlyTag, StringComparison.OrdinalIgnoreCase));

            if (!hasGuideTag && !hasFriendlyTag)
            {
                continue;
            }

            var location = CloneLocation(npc.CurrentLocation ?? npc.SpawnLocation);
            EnsureDefaults(location, world.WorldId);
            return location;
        }

        var fallback = Location.Create(new Vector3(8f, 4f, 0f), world.WorldId, DefaultMapId, DefaultZoneName);
        fallback.Direction = 180f;
        return fallback;
    }

    private static void TouchFrom(WorldState world, DateTime timestamp)
    {
        var effective = timestamp == default ? DateTime.UtcNow : timestamp;
        world.LastUpdated = effective;
    }

    private static Location CloneLocation(Location? location)
    {
        if (location is null)
        {
            return new Location();
        }

        var clone = new Location
        {
            WorldId = location.WorldId,
            MapId = location.MapId,
            MapName = location.MapName,
            Direction = location.Direction
        };

        clone.Position = location.Position;
        return clone;
    }

    private static void EnsureDefaults(Location location, Guid worldId)
    {
        location.WorldId = worldId;
        if (string.IsNullOrWhiteSpace(location.MapId))
        {
            location.MapId = DefaultMapId;
        }

        if (string.IsNullOrWhiteSpace(location.MapName))
        {
            location.MapName = DefaultZoneName;
        }
    }

    private static bool MatchesSpawnType(MapObject mapObject, string requestedSpawnType)
    {
        if (!mapObject.IsActive)
        {
            return false;
        }

        var hasSpawnTag = mapObject.Tags.Any(tag => string.Equals(tag, SpawnPointTag, StringComparison.OrdinalIgnoreCase));
        if (!hasSpawnTag)
        {
            return false;
        }

        if (mapObject.State == null || !mapObject.State.TryGetValue(SpawnTypeStateKey, out var stateValue) || string.IsNullOrWhiteSpace(stateValue))
        {
            return string.Equals(requestedSpawnType, DefaultSpawnType, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(stateValue, requestedSpawnType, StringComparison.OrdinalIgnoreCase);
    }

    private static int ParsePriority(IDictionary<string, string>? state)
    {
        if (state != null && state.TryGetValue(SpawnPriorityStateKey, out var priorityText) &&
            int.TryParse(priorityText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return int.MaxValue;
    }
}
