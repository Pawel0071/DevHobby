using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;

namespace RPG.Application.Events.RequestedEvents;

public sealed record ExperienceGainRequestedEvent(
    EventMetadata Meta,
    Guid CharacterId,
    long Amount
) : IGameEventWithMetadata
{
    public object? Payload => new { CharacterId, Amount };
    public string? PayloadType => "ExperienceGainRequested";
}

public sealed record CharacterLevelUpRequestedEvent(
    EventMetadata Meta,
    Guid CharacterId
) : IGameEventWithMetadata
{
    public object? Payload => new { CharacterId };
    public string? PayloadType => "CharacterLevelUpRequested";
}

public sealed record CharacterDeathRequestedEvent(
    EventMetadata Meta,
    Guid CharacterId,
    Guid? KillerId
) : IGameEventWithMetadata
{
    public object? Payload => new { CharacterId, KillerId };
    public string? PayloadType => "CharacterDeathRequested";
}
