using System;

namespace RPG.Domain.Models;

/// <summary>
///     Message payload describing an NPC combat skill usage for downstream consumers.
/// </summary>
public sealed record NpcSkillUsageMessage(
    Guid NpcId,
    string NpcName,
    Guid SkillId,
    string SkillName,
    Guid? TargetCharacterId,
    float PositionX,
    float PositionY,
    float PositionZ,
    float Rotation,
    DateTime OccurredAtUtc);
