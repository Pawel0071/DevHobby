using RPG.Abstractions.Interfaces;
using RPG.Domain.Models;

namespace RPG.Abstractions.SharedModel;

public sealed record NpcSkillUsedEvent(
    EventMetadata Meta,
    Guid NpcId,
    string NpcName,
    Guid SkillId,
    string SkillName,
    Guid? TargetCharacterId,
    Location? NpcLocation) : IGameEventWithMetadata
{
    public object? Payload => new { NpcId, SkillId, TargetCharacterId, NpcLocation };
    public string? PayloadType => "NpcSkillUsed";
}
