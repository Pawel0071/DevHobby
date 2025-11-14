using RPG.Abstractions.Interfaces;

namespace RPG.Application.Events;

public record ExperienceGainRequestedEvent(EventMetadata Meta, Guid CharacterId, long Amount) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, Amount }; public string? PayloadType => "ExperienceGainRequested"; }
public record CharacterLevelUpRequestedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "CharacterLevelUpRequested"; }

