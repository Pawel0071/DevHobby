using RPG.Abstractions;
using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Interfaces;

namespace RPG.Application.Dispatchers;

public sealed class NpcCombatEventDispatcher : INpcCombatEventDispatcher
{
    private readonly IGameEventDispatcher _dispatcher;

    public NpcCombatEventDispatcher(IGameEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task DispatchAsync(NpcSkillUsedEvent combatEvent, CancellationToken cancellationToken = default)
    {
        return _dispatcher.DispatchAsync(combatEvent, cancellationToken);
    }

}
