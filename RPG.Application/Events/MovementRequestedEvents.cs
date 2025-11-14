using RPG.Abstractions.Interfaces;

namespace RPG.Application.Events;

public record MovementStartRequestedEvent(EventMetadata Meta, Guid CharacterId, int Direction, bool PreserveFacing) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, Direction, PreserveFacing }; public string? PayloadType => "MovementStartRequested"; }
public record MovementStopRequestedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "MovementStopRequested"; }
public record RotationStartRequestedEvent(EventMetadata Meta, Guid CharacterId, int Direction) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, Direction }; public string? PayloadType => "RotationStartRequested"; }
public record RotationStopRequestedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "RotationStopRequested"; }

