using System.Numerics;
using RPG.Abstractions.Interfaces;

namespace RPG.Application.Events;

public record SkillUseRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid SkillId, Vector3 TargetPosition) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, SkillId, TargetPosition }; public string? PayloadType => "SkillUseRequested"; }
public record SkillLearnRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid SkillId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, SkillId }; public string? PayloadType => "SkillLearnRequested"; }
public record SkillLevelUpRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid SkillId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, SkillId }; public string? PayloadType => "SkillLevelUpRequested"; }
public record SkillUnlearnRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid SkillId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, SkillId }; public string? PayloadType => "SkillUnlearnRequested"; }

