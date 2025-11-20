namespace RPG.Application.Events;

using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Domain.Models;

public sealed record CharacterAttackRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid TargetNpcId) : IGameEventWithMetadata
{
    public object? Payload => new { CharacterId, TargetNpcId };
    public string? PayloadType => nameof(CharacterAttackRequestedEvent);
}

public sealed record NpcDamageRequestedEvent(EventMetadata Meta, Guid NpcId, Guid SourceCharacterId, float DamageAmount) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId, DamageAmount };
    public string? PayloadType => nameof(NpcDamageRequestedEvent);
}

public sealed record NpcRespawnRequestedEvent(EventMetadata Meta, Guid NpcId) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId };
    public string? PayloadType => nameof(NpcRespawnRequestedEvent);
}

