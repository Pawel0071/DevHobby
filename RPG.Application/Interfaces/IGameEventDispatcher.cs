using System.Threading;
using System.Threading.Tasks;

namespace RPG.Application.Interfaces;

public interface IGameEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent gameEvent, CancellationToken cancellationToken = default);
}
