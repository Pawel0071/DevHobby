using RPG.Abstractions.Interfaces;
using RPG.Domain.Models;

namespace RPG.Application.Events;

/// <summary>
///     Requested event when NPC AI wants to start movement to a destination.
/// </summary>
public sealed record NpcMovementStartRequestedEvent(
    EventMetadata Meta,
    Guid NpcId,
    Location Destination,
    float Speed = 1.0f
) : IGameEventWithMetadata
{
    public object Payload => new { NpcId, Destination, Speed };
    public string PayloadType => "NpcMovementStartRequested";
}

/// <summary>
///     Requested event when NPC AI wants to follow a character at a given distance.
/// </summary>
public sealed record NpcFollowTargetRequestedEvent(
    EventMetadata Meta,
    Guid NpcId,
    Guid TargetCharacterId,
    float DesiredRange = 2.0f,
    float StopDistance = 2.0f,
    float? MaxRange = null
) : IGameEventWithMetadata
{
    public object Payload => new { NpcId, TargetCharacterId, DesiredRange, StopDistance, MaxRange };
    public string PayloadType => "NpcFollowTargetRequested";
}

/// <summary>
///     Requested event when NPC AI wants to perform a combat attack against a target (optionally with a specific skill).
/// </summary>
public sealed record NpcCombatAttackRequestedEvent(
    EventMetadata Meta,
    Guid NpcId,
    Guid TargetCharacterId,
    Guid? SkillId = null
) : IGameEventWithMetadata
{
    public object Payload => new { NpcId, TargetCharacterId, SkillId };
    public string PayloadType => "NpcCombatAttackRequested";
}

/// <summary>
///     Requested event when NPC AI wants to use a specific skill (non-combat generic usage).
/// </summary>
public sealed record NpcSkillUseRequestedEvent(
    EventMetadata Meta,
    Guid NpcId,
    Guid SkillId,
    Guid? TargetId
) : IGameEventWithMetadata
{
    public object Payload => new { NpcId, SkillId, TargetId };
    public string PayloadType => "NpcSkillUseRequested";
}

/// <summary>
///     Requested event when NPC AI wants to engage a specific target (enter combat state, aggro).
/// </summary>
public sealed record NpcEngageTargetRequestedEvent(
    EventMetadata Meta,
    Guid NpcId,
    Guid TargetCharacterId
) : IGameEventWithMetadata
{
    public object Payload => new { NpcId, TargetCharacterId };
    public string PayloadType => "NpcEngageTargetRequested";
}

/// <summary>
///     Requested event when NPC AI wants to disengage from combat.
/// </summary>
public sealed record NpcDisengageRequestedEvent(
    EventMetadata Meta,
    Guid NpcId
) : IGameEventWithMetadata
{
    public object Payload => new { NpcId };
    public string PayloadType => "NpcDisengageRequested";
}

/// <summary>
///     Requested event when NPC AI wants to return to spawn location.
/// </summary>
public sealed record NpcReturnToSpawnRequestedEvent(
    EventMetadata Meta,
    Guid NpcId
) : IGameEventWithMetadata
{
    public object Payload => new { NpcId };
    public string PayloadType => "NpcReturnToSpawnRequested";
}

/// <summary>
///     Requested event when NPC AI wants to idle/wait for a duration.
/// </summary>
public sealed record NpcIdleRequestedEvent(
    EventMetadata Meta,
    Guid NpcId,
    float DurationSeconds = 0f
) : IGameEventWithMetadata
{
    public object Payload => new { NpcId, DurationSeconds };
    public string PayloadType => "NpcIdleRequested";
}

/// <summary>
///     Requested event when NPC is to be considered dead (death flow initiated by combat/logic/AI).
/// </summary>
public sealed record NpcDeathRequestedEvent(
    EventMetadata Meta,
    Guid NpcId,
    Guid? KillerId
) : IGameEventWithMetadata
{
    public object Payload => new { NpcId, KillerId };
    public string PayloadType => "NpcDeathRequested";
}

// Note: Intentionally no "final" events here. According to the architecture, RequestedEvent handlers
// (in Application.Events.Handlers) will invoke Core services, persist state, and broadcast deltas.
