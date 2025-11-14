using RPG.Abstractions.Interfaces;

namespace RPG.Application.Events;

public record CharacterLoginRequestedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "CharacterLoginRequested"; }
public record CharacterLogoutRequestedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "CharacterLogoutRequested"; }
public record CharacterDieRequestedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "CharacterDieRequested"; }

