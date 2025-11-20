using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using RPG.Core.Interfaces;
using RPG.Domain.Enums;
using RPG.Domain.Models;
using RPG.Domain.Models.MapObjects;
using RPG.Domain.Models.Npcs;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.World;

public class WorldSessionManager : IWorldSessionManager
{
    private static readonly Guid DefaultWorldId = Guid.Parse("c2bce5a0-5d6d-4eb5-8f5c-5aeb1b6f6b3d");
    private const string DefaultWorldName = "Starter Grounds";
    private const string DefaultMapId = "starter.map";
    private const string DefaultZoneName = "starter.zone";
    private const string DefaultSpawnType = "player-default";

    private readonly IModelRepository _modelRepository;
    private readonly ILogger<WorldSessionManager> _logger;
    private readonly IWorldStateService _worldStateService;
    private readonly ConcurrentDictionary<Guid, WorldState> _worldCache = new();
    private readonly ConcurrentDictionary<Guid, Guid> _sessionToWorld = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _worldLocks = new();

    public WorldSessionManager(
        IModelRepository modelRepository,
        ILogger<WorldSessionManager> logger,
        IWorldStateService worldStateService)
    {
        _modelRepository = modelRepository;
        _logger = logger;
        _worldStateService = worldStateService;
    }

    public async Task<WorldJoinResult> JoinWorldAsync(Guid sessionId, Guid? preferredWorldId, CancellationToken cancellationToken)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (!session.CharacterId.HasValue)
        {
            throw new InvalidOperationException($"Session {sessionId} has no associated character.");
        }

        var worldId = preferredWorldId ?? session.CurrentWorldId ?? DefaultWorldId;
        var world = await GetWorldReferenceAsync(worldId, true, cancellationToken).ConfigureAwait(false);
        var character = await LoadCharacterAsync(session.CharacterId.Value, cancellationToken).ConfigureAwait(false);

        var spawnLocation = new Location();
        var reuseExistingLocation = session.CurrentWorldId.HasValue && session.CurrentWorldId.Value == world.WorldId;
        var worldLock = _worldLocks.GetOrAdd(world.WorldId, _ => new SemaphoreSlim(1, 1));
        await worldLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            spawnLocation = await _worldStateService
                .DetermineSpawnLocationAsync(world, character, DefaultSpawnType, reuseExistingLocation, cancellationToken)
                .ConfigureAwait(false);
            spawnLocation.WorldId = world.WorldId;
            spawnLocation.MapId = string.IsNullOrWhiteSpace(spawnLocation.MapId) ? DefaultMapId : spawnLocation.MapId;
            spawnLocation.MapName = string.IsNullOrWhiteSpace(spawnLocation.MapName) ? DefaultZoneName : spawnLocation.MapName;

            character.CurrentLocation = spawnLocation;
            character.IsMoving = false;
            character.IsRotating = false;

            session.CurrentWorldId = world.WorldId;
            session.CurrentLocation = spawnLocation;
            session.Status = GameSessionStatus.InGame;
            session.UpdateActivity(DateTime.UtcNow, spawnLocation);

            character.IsOnline = true;
            character.IsInCombat = false;
            character.LastUpdated = DateTime.UtcNow;
            character.StatusEffects ??= new HashSet<string>();
            character.StatusEffects.Clear();

            _worldStateService.UpsertCharacter(world, character);
            _sessionToWorld[session.Id] = world.WorldId;
        }
        finally
        {
            worldLock.Release();
        }

        await _modelRepository.UpsertAsync(character, cancellationToken).ConfigureAwait(false);
        await _modelRepository.UpsertAsync(session, cancellationToken).ConfigureAwait(false);
        await PersistWorldAsync(world, cancellationToken).ConfigureAwait(false);
        var worldView = await GetWorldAsync(world.WorldId, cancellationToken).ConfigureAwait(false);

        _logger.Info($"Session {session.Id} joined world {world.WorldId} at {spawnLocation.Position}.");

        return new WorldJoinResult
        {
            World = worldView,
            SpawnLocation = spawnLocation,
            Session = session,
            Character = character
        };
    }

    public async Task LeaveWorldAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!_sessionToWorld.TryRemove(sessionId, out var worldId))
        {
            var sessionRecord = await LoadSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
            worldId = sessionRecord.CurrentWorldId ?? DefaultWorldId;
        }

        var world = await GetWorldReferenceAsync(worldId, true, cancellationToken).ConfigureAwait(false);
        var session = await LoadSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);

        var worldLock = _worldLocks.GetOrAdd(world.WorldId, _ => new SemaphoreSlim(1, 1));
        await worldLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (session.CharacterId.HasValue)
            {
                _worldStateService.RemoveCharacter(world, session.CharacterId.Value);
            }

            session.CurrentWorldId = null;
            session.CurrentLocation = null;
            session.CurrentZoneId = null;
            session.Status = GameSessionStatus.Disconnected;
        }
        finally
        {
            worldLock.Release();
        }

        await _modelRepository.UpsertAsync(session, cancellationToken).ConfigureAwait(false);
        await PersistWorldAsync(world, cancellationToken).ConfigureAwait(false);
        _logger.Info($"Session {sessionId} left world {world.WorldId}.");
    }

    public async Task<WorldState> GetWorldForSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (_sessionToWorld.TryGetValue(sessionId, out var worldId))
        {
            return await GetWorldAsync(worldId, cancellationToken).ConfigureAwait(false);
        }

        var session = await LoadSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var resolvedWorldId = session.CurrentWorldId ?? DefaultWorldId;
        return await GetWorldAsync(resolvedWorldId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorldState> GetWorldAsync(Guid worldId, CancellationToken cancellationToken)
    {
        var world = await GetWorldReferenceAsync(worldId, true, cancellationToken).ConfigureAwait(false);
        var worldLock = _worldLocks.GetOrAdd(world.WorldId, _ => new SemaphoreSlim(1, 1));
        await worldLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _worldStateService.Clone(world);
        }
        finally
        {
            worldLock.Release();
        }
    }

    public async Task UpdateCharacterAsync(Guid sessionId, Location location, CancellationToken cancellationToken)
    {
        if (!_sessionToWorld.TryGetValue(sessionId, out var worldId))
        {
            var sessionRecord = await LoadSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
            worldId = sessionRecord.CurrentWorldId ?? DefaultWorldId;
        }

        var world = await GetWorldReferenceAsync(worldId, true, cancellationToken).ConfigureAwait(false);
        var session = await LoadSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (!session.CharacterId.HasValue)
        {
            return;
        }

        var worldLock = _worldLocks.GetOrAdd(world.WorldId, _ => new SemaphoreSlim(1, 1));
        Character? characterSnapshot = null;
        await worldLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var characterId = session.CharacterId.Value;
            characterSnapshot = await LoadCharacterAsync(characterId, cancellationToken).ConfigureAwait(false);

            characterSnapshot.CurrentLocation = location;
            characterSnapshot.IsOnline = true;
            characterSnapshot.IsInCombat = session.IsInCombat;
            characterSnapshot.LastUpdated = DateTime.UtcNow;

            _worldStateService.UpsertCharacter(world, characterSnapshot);
            session.CurrentLocation = location;
            session.UpdateActivity(DateTime.UtcNow, location);
        }
        finally
        {
            worldLock.Release();
        }

        if (characterSnapshot != null)
        {
            await _modelRepository.UpsertAsync(characterSnapshot, cancellationToken).ConfigureAwait(false);
        }

        await _modelRepository.UpsertAsync(session, cancellationToken).ConfigureAwait(false);
        await PersistWorldAsync(world, cancellationToken).ConfigureAwait(false);
    }

    private async Task<GameSession> LoadSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _modelRepository.GetByIdAsync<GameSession>(sessionId, cancellationToken).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found.");
        }

        return session;
    }

    private async Task<Character> LoadCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _modelRepository.GetByIdAsync<Character>(characterId, cancellationToken).ConfigureAwait(false);
        if (character == null)
        {
            throw new InvalidOperationException($"Character {characterId} not found.");
        }

        return character;
    }

    private async Task<WorldState?> LoadWorldFromRepositoryAsync(Guid worldId, CancellationToken cancellationToken)
    {
        var world = await _modelRepository.GetByIdAsync<WorldState>(worldId, cancellationToken).ConfigureAwait(false);
        if (world != null)
        {
            return world;
        }

        var worlds = await _modelRepository.GetAllAsync<WorldState>(cancellationToken).ConfigureAwait(false);
        return worlds.FirstOrDefault(w => w.WorldId == worldId);
    }

    private async Task<WorldState> BuildDefaultWorldAsync(Guid worldId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var id = worldId;
        var guideLocation = Location.Create(new Vector3(12, 5, 0), worldId, "starter.map", "starter.zone");
        var guideNpc = Npc.Create("starter.npc.tutorial-guide", "Tutorial Guide", CloneLocation(guideLocation), worldId,
            new HashSet<string> { "guide", "quest" });
        typeof(Npc).GetProperty("Id")!.SetValue(guideNpc, Guid.Parse("3fd82816-3cda-47c4-a0fb-12fbc9d795d4"));
        guideNpc.SetCurrentLocation(guideLocation);
        guideNpc.LastUpdated = now;

        var campfireLocation = Location.Create(new Vector3(10, 5, 0), worldId, "starter.map", "starter.zone");
        var campfire = MapObject.Create("starter.campfire", campfireLocation, worldId, campfireLocation.MapName);
        typeof(MapObject).GetProperty("Id")!.SetValue(campfire, Guid.Parse("278b7195-6225-40ba-8af4-cdb339e64512"));
        campfire.DisplayName = "Campfire";
        campfire.IsActive = true;
        campfire.LastUpdated = now;
        campfire.Tags = new HashSet<string> { "rest", "starter" };
        campfire.State = new Dictionary<string, string> { { "temperature", "warm" } };

        var spawnLocation = Location.Create(new Vector3(8f, 4f, 0f), worldId, DefaultMapId, DefaultZoneName);
        spawnLocation.Direction = 180f;

        var spawnPoint = MapObject.Create("starter.spawn.default", CloneLocation(spawnLocation), worldId, DefaultZoneName);
        spawnPoint.DisplayName = "Arrival Beacon";
        spawnPoint.IsActive = true;
        spawnPoint.LastUpdated = now;
        spawnPoint.Tags = new HashSet<string> { "spawn-point", "player" };
        spawnPoint.State = new Dictionary<string, string>
        {
            ["spawnType"] = DefaultSpawnType,
            ["priority"] = "0",
            ["rotation"] = spawnLocation.Direction.ToString(CultureInfo.InvariantCulture)
        };

        var world = WorldState.Hydrate(id, worldId, DefaultWorldName, now);
        _worldStateService.UpsertNpc(world, guideNpc);
        _worldStateService.UpsertMapObject(world, campfire);
        _worldStateService.UpsertMapObject(world, spawnPoint);
        _worldStateService.Touch(world, now);

        await _modelRepository.UpsertAsync(guideNpc, cancellationToken).ConfigureAwait(false);
        await _modelRepository.UpsertAsync(campfire, cancellationToken).ConfigureAwait(false);
        await _modelRepository.UpsertAsync(spawnPoint, cancellationToken).ConfigureAwait(false);
        await PersistWorldAsync(world, cancellationToken).ConfigureAwait(false);
        return world;
    }

    private static Location CloneLocation(Location location)
    {
        var clone = Location.Create(location.Position, location.WorldId, location.MapId, location.MapName);
        clone.Direction = location.Direction;
        return clone;
    }

    private async Task<WorldState> GetWorldReferenceAsync(Guid worldId, bool createIfMissing, CancellationToken cancellationToken)
    {
        var worldLock = _worldLocks.GetOrAdd(worldId, _ => new SemaphoreSlim(1, 1));
        await worldLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_worldCache.TryGetValue(worldId, out var cached))
            {
                return cached;
            }

            var world = await LoadWorldFromRepositoryAsync(worldId, cancellationToken).ConfigureAwait(false);
            if (world == null)
            {
                if (!createIfMissing)
                {
                    throw new InvalidOperationException($"World {worldId} not found.");
                }

                world = await BuildDefaultWorldAsync(worldId, cancellationToken).ConfigureAwait(false);
            }

            _worldCache[world.WorldId] = world;
            return world;
        }
        finally
        {
            worldLock.Release();
        }
    }

    private async Task PersistWorldAsync(WorldState world, CancellationToken cancellationToken)
    {
        _worldStateService.Touch(world, DateTime.UtcNow);
        await _modelRepository.UpsertAsync(world, cancellationToken).ConfigureAwait(false);
        _worldCache[world.WorldId] = world;
    }
}
