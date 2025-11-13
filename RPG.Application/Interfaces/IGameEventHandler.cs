using System.Threading;
using System.Threading.Tasks;
using RPG.Abstractions.Interfaces;

namespace RPG.Application.Interfaces;

public interface IGameEventHandler<in TEvent> where TEvent : IGameEvent
{
    Task HandleAsync(TEvent gameEvent, CancellationToken cancellationToken);
}
