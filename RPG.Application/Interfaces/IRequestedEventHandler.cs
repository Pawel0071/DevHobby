// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Interfaces/IRequestedEventHandler.cs
using System.Threading;
using System.Threading.Tasks;
using RPG.Abstractions.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Interfaces;

public interface IRequestedEventHandler
{
    /// <summary>
    /// Typ eventu, który ten handler obsługuje (dokładnie jeden RequestedEvent 1:1).
    /// Jeśli handler obsługuje wiele typów, użyj CanHandle zamiast tego.
    /// </summary>
    Type EventType { get; }

    /// <summary>
    /// Sprawdza czy handler może obsłużyć dany event.
    /// Używane gdy handler obsługuje wiele typów eventów.
    /// </summary>
    bool CanHandle(IGameEvent evt);

    Task HandleAsync(IGameEvent evt, CancellationToken ct);
}
