using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Domain.Models;
using RPG.Domain.Models.Skills;

namespace RPG.Application.Events.RequestedEvents;

public sealed record SkillUsageRequestedEvent(
    EventMetadata Meta,
    Guid CharacterId,
    Guid SkillId,
    Guid? TargetId
) : IGameEventWithMetadata
{
    public object? Payload => new { CharacterId, SkillId, TargetId };
    public string? PayloadType => "SkillUsageRequested";
}

public sealed record SkillLearnRequestedEvent(
    EventMetadata Meta,
    Guid CharacterId,
    Skill Skill
) : IGameEventWithMetadata
{
    public object? Payload => new { CharacterId, SkillId = Skill.Id };
    public string? PayloadType => "SkillLearnRequested";
}

public sealed record SkillLevelUpRequestedEvent(
    EventMetadata Meta,
    Guid CharacterId,
    Guid SkillId
) : IGameEventWithMetadata
{
    public object? Payload => new { CharacterId, SkillId };
    public string? PayloadType => "SkillLevelUpRequested";
}

public sealed record SkillUnlearnRequestedEvent(
    EventMetadata Meta,
    Guid CharacterId,
    Guid SkillId
) : IGameEventWithMetadata
{
    public object? Payload => new { CharacterId, SkillId };
    public string? PayloadType => "SkillUnlearnRequested";
}

