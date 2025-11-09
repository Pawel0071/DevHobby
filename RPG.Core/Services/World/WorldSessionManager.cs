using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using RPG.Core.Interfaces;
using RPG.Domain.Entities;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Enums;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.World;

public class WorldSessionManager : IWorldSessionManager
{
    private static readonly Guid DefaultWorldId = Guid.Parse("c2bce5a0-5d6d-4eb5-8f5c-5aeb1b6f6b3d");
    private const string DefaultWorldName = "Starter Grounds";

    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<WorldSessionManager> _logger;
    private readonly IWorldStateService _worldStateService;
    private readonly ConcurrentDictionary<Guid, WorldState> _worldCache = new();
    private readonly ConcurrentDictionary<Guid, Guid> _sessionToWorld = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _worldLocks = new();

    public WorldSessionManager(
        IDocumentRepository documentRepository,
        ILogger<WorldSessionManager> logger,
        IWorldStateService worldStateService)
    {
        _documentRepository = documentRepository;
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
        var worldLock = _worldLocks.GetOrAdd(world.WorldId, _ => new SemaphoreSlim(1, 1));
        await worldLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            spawnLocation = DetermineSpawnLocation(world, character);
            spawnLocation.WorldId ??= world.WorldId;
            spawnLocation.MapId = string.IsNullOrWhiteSpace(spawnLocation.MapId) ? "starter.map" : spawnLocation.MapId;
            spawnLocation.ZoneName = string.IsNullOrWhiteSpace(spawnLocation.ZoneName) ? "starter.zone" : spawnLocation.ZoneName;

            character.SetCurrentLocation(spawnLocation);
            character.SetMovementState(false);
            character.SetRotationState(false);

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

        await _documentRepository.UpsertAsync(character, cancellationToken).ConfigureAwait(false);
        await _documentRepository.UpsertAsync(session, cancellationToken).ConfigureAwait(false);
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

        await _documentRepository.UpsertAsync(session, cancellationToken).ConfigureAwait(false);
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
        await worldLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = world.Characters.FirstOrDefault(c => c.Id == session.CharacterId.Value);
            Character characterSnapshot;
            if (existing != null)
            {
                characterSnapshot = existing;
            }
            else
            {
                characterSnapshot = await LoadCharacterAsync(session.CharacterId.Value, cancellationToken).ConfigureAwait(false);
            }

            characterSnapshot.SetCurrentLocation(location);
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

        await _documentRepository.UpsertAsync(session, cancellationToken).ConfigureAwait(false);
        await PersistWorldAsync(world, cancellationToken).ConfigureAwait(false);
    }

    private async Task<GameSession> LoadSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _documentRepository.GetByIdAsync<GameSession>(sessionId, cancellationToken).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found.");
        }

        return session;
    }

    private async Task<Character> LoadCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _documentRepository.GetByIdAsync<Character>(characterId, cancellationToken).ConfigureAwait(false);
        if (character == null)
        {
            throw new InvalidOperationException($"Character {characterId} not found.");
        }

        return character;
    }

    private async Task<WorldState?> LoadWorldFromRepositoryAsync(Guid worldId, CancellationToken cancellationToken)
    {
        var world = await _documentRepository.GetByIdAsync<WorldState>(worldId, cancellationToken).ConfigureAwait(false);
        if (world != null)
        {
            return world;
        }

        var worlds = await _documentRepository.GetAllAsync<WorldState>(cancellationToken).ConfigureAwait(false);
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
        guideNpc.IsAlive = true;
        guideNpc.LastUpdated = now;

        var campfireLocation = Location.Create(new Vector3(10, 5, 0), worldId, "starter.map", "starter.zone");
        var campfire = MapObject.Create("starter.campfire", campfireLocation, worldId, campfireLocation.ZoneName);
        typeof(MapObject).GetProperty("Id")!.SetValue(campfire, Guid.Parse("278b7195-6225-40ba-8af4-cdb339e64512"));
        campfire.DisplayName = "Campfire";
        campfire.IsActive = true;
        campfire.LastUpdated = now;
        campfire.Tags = new HashSet<string> { "rest", "starter" };
        campfire.State = new Dictionary<string, string> { { "temperature", "warm" } };

        var world = WorldState.Hydrate(
            id,
            worldId,
            DefaultWorldName,
            now,
            Array.Empty<Character>(),
            new[] { guideNpc },
            new[] { campfire });

        await PersistWorldAsync(world, cancellationToken).ConfigureAwait(false);
        return world;
    }

    private static Location CloneLocation(Location location)
    {
        var clone = Location.Create(location.Position, location.WorldId ?? DefaultWorldId, location.MapId, location.ZoneName);
        clone.Rotation = location.Rotation;
        return clone;
    }

    private static Location DetermineSpawnLocation(WorldState world, Character character)
    {
        var currentLocation = character.CurrentLocation;
        if (currentLocation != null && currentLocation.WorldId == world.WorldId)
        {
            return Location.Create(currentLocation.Position, world.WorldId, currentLocation.MapId, currentLocation.ZoneName);
        }

        var spawn = Location.Create(new Vector3(8f, 4f, 0f), world.WorldId, "starter.map", "starter.zone");
        spawn.Rotation = 180f;
        return spawn;
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
        await _documentRepository.UpsertAsync(world, cancellationToken).ConfigureAwait(false);
        _worldCache[world.WorldId] = world;
    }
}
