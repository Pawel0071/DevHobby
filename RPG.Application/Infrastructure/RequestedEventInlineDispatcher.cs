// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Infrastructure/RequestedEventInlineDispatcher.cs
using Microsoft.Extensions.DependencyInjection;
using RPG.Abstractions.Interfaces;
using RPG.Application.Interfaces;

namespace RPG.Application.Infrastructure;

public interface IRequestedEventInlineDispatcher
{
    Task<bool> TryHandleAsync(IGameEventWithMetadata evt, CancellationToken ct);
}

public sealed class RequestedEventInlineDispatcher : IRequestedEventInlineDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RequestedEventInlineDispatcher(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> TryHandleAsync(IGameEventWithMetadata evt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetRequiredService<IEnumerable<IRequestedEventHandler>>();
        foreach (var h in handlers)
        {
            if (!h.CanHandle(evt)) continue;
            await h.HandleAsync(evt, ct);
            return true;
        }
        return false;
    }
}

