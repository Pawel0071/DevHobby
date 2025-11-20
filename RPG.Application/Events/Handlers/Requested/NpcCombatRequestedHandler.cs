using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Events;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Models;
using RPG.Domain.Models.Npcs;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Logger;

namespace RPG.Application.Events.Handlers.Requested;

public sealed class NpcCombatRequestedHandler : IRequestedEventHandler
{
    private readonly IModelRepository _repository;
    private readonly ICombatService _combatService;
    private readonly IGameStateBroadcaster _broadcaster;
    private readonly ILogger<NpcCombatRequestedHandler> _logger;

    public NpcCombatRequestedHandler(
        IModelRepository repository,
        ICombatService combatService,
        IGameStateBroadcaster broadcaster,
        ILogger<NpcCombatRequestedHandler> logger)
    {
        _repository = repository;
        _combatService = combatService;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public Type EventType { get; }

    public bool CanHandleAsync(IGameEvent evt)
    {
        // TO DO - add other NPC combat events if needed
        return true;
    }

    public bool CanHandle(IGameEvent evt)
    {
        // TO DO - add other NPC combat events if needed
        throw new NotImplementedException();
    }

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct = default)
    {
        if (evt is not NpcCombatAttackRequestedEvent e)
            return;

        _logger.Info($"NPC {e.NpcId} attacking character {e.TargetCharacterId}");

        var npc = await _repository.GetByIdAsync<Npc>(e.NpcId, ct);
        if (npc == null)
        {
            _logger.Warn($"NPC {e.NpcId} not found");
            return;
        }

        var target = await _repository.GetByIdAsync<Character>(e.TargetCharacterId, ct);
        if (target == null)
        {
            _logger.Warn($"Character {e.TargetCharacterId} not found");
            return;
        }

        var result = await _combatService.MeleeAttackAsync(npc, target);
        if (!result.Success)
        {
            _logger.Warn($"Combat failed: {result.Message}");
            return;
        }

        await _repository.UpsertAsync(target, ct);
        _logger.Info($"NPC combat result: {result.Message}, target health: {target.CurrentHealth}");

        // Broadcast character health change
        var delta = new GameDeltaUpdate
        {
            WorldId = target.CurrentLocation?.WorldId ?? Guid.Empty,
            CharacterChanges = new List<CharacterDelta>
            {
                // TO DO - consider only sending health change deltas
                new ()
                /* TO DO - expand with more fields if needed {
                    CharacterId = target.Id,
                    CurrentHealth = target.CurrentHealth,
                    IsAlive = target.IsAlive
                } */

            }
        };
        await _broadcaster.BroadcastDeltaAsync(delta, ct);

        // If target died, generate death event
        if (!target.IsAlive)
        {
            _logger.Info($"Character {target.Id} died from NPC attack");
            // Death handler will take care of respawn logic
        }
    }
}

