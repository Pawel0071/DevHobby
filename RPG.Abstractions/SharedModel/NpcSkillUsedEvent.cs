using RPG.Domain.Models;

namespace RPG.Abstractions.SharedModel;

/// <summary>
///     Emitted when an NPC executes a combat skill.
/// </summary>
public sealed record NpcSkillUsedEvent(
    Guid NpcId,
    string NpcName,
    Guid SkillId,
    string SkillName,
    Guid? TargetCharacterId,
    Location? NpcLocation,
    DateTime OccurredAtUtc);
