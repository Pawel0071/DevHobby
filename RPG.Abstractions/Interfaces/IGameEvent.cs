namespace RPG.Abstractions.Interfaces;

/// <summary>
/// Marker interface for all game domain events (commands produce events; events are facts).
/// </summary>
public interface IGameEvent { }

public sealed record EventMetadata(
    Guid EventId,
    Guid CorrelationId,
    Guid? CausationId,
    int Sequence,
    DateTime OccurredAtUtc);

public sealed record CommandMetadata(
    Guid CommandId,
    Guid CorrelationId,
    Guid? CausationId,
    DateTime OccurredAtUtc);

public interface IGameEventWithMetadata : IGameEvent
{
    EventMetadata Meta { get; }
    object? Payload { get; }
    string? PayloadType { get; }
}
