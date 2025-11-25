using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Events.RequestedEvents;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Events.Handlers.Requested;

public sealed class CombatRequestedHandler : IRequestedEventHandler
{
    private readonly IModelRepository _repository;
    private readonly ICombatService _combatService;
    private readonly IGameStateBroadcaster _broadcaster;

    public CombatRequestedHandler(IModelRepository repository, ICombatService combatService, IGameStateBroadcaster broadcaster)
    {
        _repository = repository;
        _combatService = combatService;
        _broadcaster = broadcaster;
    }

    public Type EventType => typeof(MeleeAttackRequestedEvent); // primary type marker, CanHandle covers all
    public bool CanHandle(IGameEvent evt) => evt is MeleeAttackRequestedEvent or RangedAttackRequestedEvent or SkillAttackRequestedEvent;

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct)
    {
        switch (evt)
        {
            case MeleeAttackRequestedEvent m:
                await HandleMeleeAsync(m, ct).ConfigureAwait(false);
                break;
            case RangedAttackRequestedEvent r:
                await HandleRangedAsync(r, ct).ConfigureAwait(false);
                break;
            case SkillAttackRequestedEvent s:
                await HandleSkillAsync(s, ct).ConfigureAwait(false);
                break;
        }
    }

    private async Task HandleMeleeAsync(MeleeAttackRequestedEvent e, CancellationToken ct)
    {
        var attacker = await _repository.GetByIdAsync<Character>(e.AttackerId, ct).ConfigureAwait(false);
        var target = await _repository.GetByIdAsync<Character>(e.TargetId, ct).ConfigureAwait(false);
        if (attacker is null || target is null) return;
        var result = await _combatService.MeleeAttackAsync(attacker, target).ConfigureAwait(false);
        if (!result.Success) return;
        await PersistAndBroadcastAsync(attacker, target, ct).ConfigureAwait(false);
    }

    private async Task HandleRangedAsync(RangedAttackRequestedEvent e, CancellationToken ct)
    {
        var attacker = await _repository.GetByIdAsync<Character>(e.AttackerId, ct).ConfigureAwait(false);
        var target = await _repository.GetByIdAsync<Character>(e.TargetId, ct).ConfigureAwait(false);
        if (attacker is null || target is null) return;
        var result = await _combatService.RangeAttackAsync(attacker, target).ConfigureAwait(false);
        if (!result.Success) return;
        await PersistAndBroadcastAsync(attacker, target, ct).ConfigureAwait(false);
    }

    private async Task HandleSkillAsync(SkillAttackRequestedEvent e, CancellationToken ct)
    {
        var attacker = await _repository.GetByIdAsync<Character>(e.AttackerId, ct).ConfigureAwait(false);
        var target = await _repository.GetByIdAsync<Character>(e.TargetId, ct).ConfigureAwait(false);
        if (attacker is null || target is null) return;
        var result = await _combatService.SkillAttackAsync(attacker, target, e.SkillId).ConfigureAwait(false);
        if (!result.Success) return;
        await PersistAndBroadcastAsync(attacker, target, ct).ConfigureAwait(false);
    }

    private async Task PersistAndBroadcastAsync(Character attacker, Character target, CancellationToken ct)
    {
        await _repository.UpsertAsync(attacker, ct).ConfigureAwait(false);
        await _repository.UpsertAsync(target, ct).ConfigureAwait(false);

        var worldId = attacker.CurrentLocation?.WorldId ?? target.CurrentLocation?.WorldId ?? Guid.Empty;
        var delta = new GameDeltaUpdate
        {
            WorldId = worldId,
            CharacterChanges = new List<CharacterDelta>
            {
                new CharacterDelta { CharacterId = attacker.Id, Location = attacker.CurrentLocation },
                new CharacterDelta { CharacterId = target.Id, Location = target.CurrentLocation }
            }
        };
        await _broadcaster.BroadcastDeltaAsync(delta, ct).ConfigureAwait(false);
    }
}
