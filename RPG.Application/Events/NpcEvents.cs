using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Events;
using RPG.Domain.Models;

namespace RPG.Application.Events;

/// <summary>
///     Requested event when NPC AI wants to move to a location.
/// </summary>
public sealed record NpcMoveRequestedEvent(
    EventMetadata Meta,
    Guid NpcId,
    Location Destination,
    float Speed = 1.0f
) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId, Destination, Speed };
    public string? PayloadType => "NpcMoveRequested";
}

/// <summary>
///     Requested event when NPC AI wants to use a skill.
/// </summary>
public sealed record NpcSkillUseRequestedEvent(
    EventMetadata Meta,
    Guid NpcId,
    Guid SkillId,
    Guid? TargetId
) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId, SkillId, TargetId };
    public string? PayloadType => "NpcSkillUseRequested";
}

/// <summary>
///     Requested event when NPC AI wants to engage target.
/// </summary>
public sealed record NpcEngageTargetRequestedEvent(
    EventMetadata Meta,
    Guid NpcId,
    Guid TargetCharacterId
) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId, TargetCharacterId };
    public string? PayloadType => "NpcEngageTargetRequested";
}

/// <summary>
///     Requested event when NPC AI wants to disengage from combat.
/// </summary>
public sealed record NpcDisengageRequestedEvent(
    EventMetadata Meta,
    Guid NpcId
) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId };
    public string? PayloadType => "NpcDisengageRequested";
}

/// <summary>
///     Requested event when NPC AI wants to return to spawn.
/// </summary>
public sealed record NpcReturnToSpawnRequestedEvent(
    EventMetadata Meta,
    Guid NpcId
) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId };
    public string? PayloadType => "NpcReturnToSpawnRequested";
}

/// <summary>
///     Requested event when NPC AI wants to idle/wait.
/// </summary>
public sealed record NpcIdleRequestedEvent(
    EventMetadata Meta,
    Guid NpcId,
    float DurationSeconds = 0f
) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId, DurationSeconds };
    public string? PayloadType => "NpcIdleRequested";
}

/// <summary>
///     Final event when NPC actually moved.
/// </summary>
public sealed record NpcMovedEvent(
    EventMetadata Meta,
    Guid NpcId,
    Location NewLocation
) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId, NewLocation };
    public string? PayloadType => "NpcMoved";
}

/// <summary>
///     Final event when NPC used a skill.
/// </summary>
public sealed record NpcSkillUsedEvent(
    EventMetadata Meta,
    Guid NpcId,
    Guid SkillId,
    Guid? TargetId
) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId, SkillId, TargetId };
    public string? PayloadType => "NpcSkillUsed";
}

/// <summary>
///     Final event when NPC engaged a target.
/// </summary>
public sealed record NpcEngagedTargetEvent(
    EventMetadata Meta,
    Guid NpcId,
    Guid TargetCharacterId
) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId, TargetCharacterId };
    public string? PayloadType => "NpcEngagedTarget";
}

/// <summary>
///     Final event when NPC died.
/// </summary>
public sealed record NpcDiedEvent(
    EventMetadata Meta,
    Guid NpcId,
    Guid? KillerId
) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId, KillerId };
    public string? PayloadType => "NpcDied";
}

