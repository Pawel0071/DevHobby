using RPG.Abstractions.Interfaces;
using RPG.Application.Diagnostics;
using RPG.Application.Infrastructure;
using RPG.Infrastructure.Interfaces;
using System.Diagnostics;

namespace RPG.Application.Dispatchers;

public sealed class BroadcastingEventDispatcher : IGameEventDispatcher
{
    private readonly IGameEventDispatcher _inner;
    private readonly IEventBroadcaster _broadcaster;
    private readonly ILogger<BroadcastingEventDispatcher> _logger;

    public BroadcastingEventDispatcher(IGameEventDispatcher inner, IEventBroadcaster broadcaster, ILogger<BroadcastingEventDispatcher> logger)
    {
        _inner = inner;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public async Task DispatchAsync<TEvent>(TEvent gameEvent, CancellationToken cancellationToken) where TEvent : IGameEvent
    {
        await _inner.DispatchAsync(gameEvent, cancellationToken).ConfigureAwait(false);
        if (gameEvent is IGameEventWithMetadata meta)
        {
            using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("EventBroadcast");
            activity?.SetTag("rpg.event.type", meta.PayloadType);
            _logger.Debug($"Broadcasting event {meta.PayloadType} corr={meta.Meta.CorrelationId} seq={meta.Meta.Sequence}");
            await _broadcaster.PublishAsync(meta, cancellationToken).ConfigureAwait(false);
        }
    }
}
