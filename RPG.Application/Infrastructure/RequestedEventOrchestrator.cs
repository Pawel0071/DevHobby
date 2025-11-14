using Microsoft.Extensions.DependencyInjection;
using RPG.Abstractions.Interfaces;
using RPG.Application.Interfaces;

namespace RPG.Application.Infrastructure;

/// <summary>
/// Orchestrator 1:1 dla RequestedEventów – mapuje typ eventu na jeden IRequestedEventHandler.
/// Ukrywa szczegóły rejestracji handlerów w DI; HostedService i inline dispatcher
/// widzą tylko ten orchestrator.
/// </summary>
public interface IRequestedEventOrchestrator
{
    Task<bool> TryHandleAsync(IGameEventWithMetadata evt, CancellationToken ct);
}

public sealed class RequestedEventOrchestrator : IRequestedEventOrchestrator
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RequestedEventOrchestrator(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> TryHandleAsync(IGameEventWithMetadata evt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetRequiredService<IEnumerable<IRequestedEventHandler>>().ToList();

        // Najpierw sprawdź CanHandle, potem EventType (dla handlerow obsługujących wiele typów)
        var handler = handlers.FirstOrDefault(h => h.CanHandle(evt))
                      ?? handlers.FirstOrDefault(h => h.EventType == evt.GetType());

        if (handler is null)
            return false;

        await handler.HandleAsync(evt, ct);
        return true;
    }
}

