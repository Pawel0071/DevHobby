using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Models.Npcs;
using RPG.Infrastructure.Interfaces;
using RPG.Domain.Models; // dodane dla Location

namespace RPG.Application.Events.Handlers;

/// <summary>
///     Handles NPC AI movement requests.
/// </summary>
public sealed class NpcMovementRequestedHandler : IRequestedEventHandler
{
    private readonly IModelRepository _npcRepo;
    private readonly IMovementService _movementService;
    private readonly IGameStateBroadcaster _broadcaster;
    private readonly ILogger<NpcMovementRequestedHandler> _logger;

    public NpcMovementRequestedHandler(
        IModelRepository npcRepo,
        IMovementService movementService,
        IGameStateBroadcaster broadcaster,
        ILogger<NpcMovementRequestedHandler> logger)
    {
        _npcRepo = npcRepo;
        _movementService = movementService;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public Type EventType => typeof(NpcMoveRequestedEvent);
    public bool CanHandle(IGameEvent evt) => evt is NpcMoveRequestedEvent;

    public async Task HandleAsync(IGameEvent gameEvent, CancellationToken ct = default)
    {
        if (gameEvent is not NpcMoveRequestedEvent e)
            return;

        var npc = await _npcRepo.GetByIdAsync<Npc>(e.NpcId, ct);
        if (npc is null)
        {
            _logger.Warn($"NPC {e.NpcId} not found for movement request");
            return;
        }

        var direction = new System.Numerics.Vector3(
            e.Destination.Position.X - npc.CurrentLocation.Position.X,
            e.Destination.Position.Y - npc.CurrentLocation.Position.Y,
            e.Destination.Position.Z - npc.CurrentLocation.Position.Z);
        var moveResult = _movementService.Move(npc, direction, 1f);
        if (!moveResult.Success)
        {
            _logger.Warn($"NPC {e.NpcId} movement failed: {moveResult.Message}");
            return;
        }

        npc.SetCurrentLocation(e.Destination); // docelowa lokacja (teleport/jednokrok)
        npc.SetMovementState(true);
        npc.LastUpdated = DateTime.UtcNow;
        await _npcRepo.UpsertAsync(npc, ct);

        var delta = new NpcDelta
        {
            NpcId = e.NpcId,
            Location = e.Destination,
            IsAlive = npc.CurrentHealth > 0
        };

        await _broadcaster.BroadcastDeltaAsync(
            new GameDeltaUpdate { WorldId = npc.WorldId, NpcChanges = new[] { delta } }, ct);

        _logger.Debug($"NPC {e.NpcId} moved to {e.Destination}");
    }
}

/// <summary>
///     Handles NPC idle requests.
/// </summary>
public sealed class NpcIdleRequestedHandler : IRequestedEventHandler
{
    private readonly IModelRepository _npcRepo;
    private readonly ILogger<NpcIdleRequestedHandler> _logger;

    public NpcIdleRequestedHandler(IModelRepository npcRepo, ILogger<NpcIdleRequestedHandler> logger)
    {
        _npcRepo = npcRepo;
        _logger = logger;
    }

    public Type EventType => typeof(NpcIdleRequestedEvent);
    public bool CanHandle(IGameEvent evt) => evt is NpcIdleRequestedEvent;

    public async Task HandleAsync(IGameEvent gameEvent, CancellationToken ct = default)
    {
        if (gameEvent is not NpcIdleRequestedEvent e)
            return;

        var npc = await _npcRepo.GetByIdAsync<Npc>(e.NpcId, ct);
        if (npc is null)
        {
            _logger.Warn($"NPC {e.NpcId} not found for idle request");
            return;
        }

        npc.SetMovementState(false);
        npc.LastUpdated = DateTime.UtcNow;

        await _npcRepo.UpsertAsync(npc, ct);

        _logger.Debug($"NPC {e.NpcId} is now idle");
    }
}

/// <summary>
///     Handles NPC return to spawn requests.
/// </summary>
public sealed class NpcReturnToSpawnRequestedHandler : IRequestedEventHandler
{
    private readonly IModelRepository _npcRepo;
    private readonly IMovementService _movementService;
    private readonly IGameStateBroadcaster _broadcaster;
    private readonly ILogger<NpcReturnToSpawnRequestedHandler> _logger;

    public NpcReturnToSpawnRequestedHandler(
        IModelRepository npcRepo,
        IMovementService movementService,
        IGameStateBroadcaster broadcaster,
        ILogger<NpcReturnToSpawnRequestedHandler> logger)
    {
        _npcRepo = npcRepo;
        _movementService = movementService;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public Type EventType => typeof(NpcReturnToSpawnRequestedEvent);
    public bool CanHandle(IGameEvent evt) => evt is NpcReturnToSpawnRequestedEvent;

    public async Task HandleAsync(IGameEvent gameEvent, CancellationToken ct = default)
    {
        if (gameEvent is not NpcReturnToSpawnRequestedEvent e)
            return;

        var npc = await _npcRepo.GetByIdAsync<Npc>(e.NpcId, ct);
        if (npc is null)
        {
            _logger.Warn($"NPC {e.NpcId} not found for return to spawn request");
            return;
        }

        var direction = new System.Numerics.Vector3(
            npc.SpawnLocation.Position.X - npc.CurrentLocation.Position.X,
            npc.SpawnLocation.Position.Y - npc.CurrentLocation.Position.Y,
            npc.SpawnLocation.Position.Z - npc.CurrentLocation.Position.Z);
        var moveResult = _movementService.Move(npc, direction, 1f);
        if (!moveResult.Success)
        {
            _logger.Warn($"NPC {e.NpcId} return to spawn failed: {moveResult.Message}");
            return;
        }

        npc.SetCurrentLocation(npc.SpawnLocation);
        npc.SetMovementState(false);
        npc.CurrentHealth = npc.MaxHealth;
        npc.LastUpdated = DateTime.UtcNow;
        await _npcRepo.UpsertAsync(npc, ct);

        var delta = new NpcDelta
        {
            NpcId = e.NpcId,
            Location = npc.SpawnLocation,
            IsAlive = true
        };

        await _broadcaster.BroadcastDeltaAsync(
            new GameDeltaUpdate { WorldId = npc.WorldId, NpcChanges = new[] { delta } }, ct);

        _logger.Info($"NPC {e.NpcId} returned to spawn");
    }
}

// Usunięty lokalny rekord NpcDelta (duplikat SharedModel.NpcDelta)
