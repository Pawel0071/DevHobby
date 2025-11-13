using System.Threading;
using System.Threading.Tasks;

namespace RPG.Abstractions.Interfaces;

public interface IGameEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent gameEvent, CancellationToken cancellationToken) where TEvent : IGameEvent;
}
