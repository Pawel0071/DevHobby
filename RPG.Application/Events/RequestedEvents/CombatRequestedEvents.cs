using RPG.Abstractions.Interfaces;

namespace RPG.Application.Events.RequestedEvents;

public sealed record MeleeAttackRequestedEvent(EventMetadata Meta, Guid AttackerId, Guid TargetId) : IGameEventWithMetadata
{
    public object Payload => new { AttackerId, TargetId };
    public string PayloadType => nameof(MeleeAttackRequestedEvent);
}

public sealed record RangedAttackRequestedEvent(EventMetadata Meta, Guid AttackerId, Guid TargetId) : IGameEventWithMetadata
{
    public object Payload => new { AttackerId, TargetId };
    public string PayloadType => nameof(RangedAttackRequestedEvent);
}

public sealed record SkillAttackRequestedEvent(EventMetadata Meta, Guid AttackerId, Guid TargetId, Guid SkillId) : IGameEventWithMetadata
{
    public object Payload => new { AttackerId, TargetId, SkillId };
    public string PayloadType => nameof(SkillAttackRequestedEvent);
}
