using System.Threading;
using System.Threading.Tasks;

namespace RPG.Application.Interfaces;

public interface IGameEventHandler<in TEvent>
{
    Task HandleAsync(TEvent gameEvent, CancellationToken cancellationToken = default);
}
