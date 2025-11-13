using RPG.Abstractions.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Infrastructure;

public interface IEventBroadcaster
{
    Task PublishAsync(IGameEventWithMetadata evt, CancellationToken ct);
}

public sealed class LoggingEventBroadcaster : IEventBroadcaster
{
    private readonly ILogger<LoggingEventBroadcaster> _logger;
    public LoggingEventBroadcaster(ILogger<LoggingEventBroadcaster> logger) => _logger = logger;
    public Task PublishAsync(IGameEventWithMetadata evt, CancellationToken ct)
    {
        _logger.Debug($"Broadcast payloadType={evt.PayloadType} corr={evt.Meta.CorrelationId} seq={evt.Meta.Sequence}");
        return Task.CompletedTask;
    }
}
