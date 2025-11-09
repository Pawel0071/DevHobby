using System.Threading;
using System.Threading.Tasks;
using RPG.Abstractions.SharedModel;

namespace RPG.Abstractions.Interfaces;

public interface INpcCombatEventDispatcher
{
    Task DispatchAsync(NpcSkillUsedEvent combatEvent, CancellationToken cancellationToken = default);
}
