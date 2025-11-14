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
    private readonly IRequestedEventOrchestrator _orchestrator;

    public RequestedEventInlineDispatcher(IRequestedEventOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public Task<bool> TryHandleAsync(IGameEventWithMetadata evt, CancellationToken ct)
        => _orchestrator.TryHandleAsync(evt, ct);
}
