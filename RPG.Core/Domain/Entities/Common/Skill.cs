using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Domain.Interfaces;

namespace RPG.Core.Domain.Entities.Common;

public class Skill
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public SkillType Type { get; set; }
    public int Level { get; set; }
    public int ResourceCost { get; set; }
    public TimeSpan CastTime { get; set; } 
    public float Cooldown { get; set; }
    public int BasePower { get; set; } 
    public float Range { get; set; } 
    public TimeSpan Duration { get; set; } 
    public string? Description { get; set; }
    public IStatsContainer? AppliedModificators { get; set; }
    public bool CanUseWhileMoving => CastTime == TimeSpan.Zero;
    public bool RequiresTarget => Type != SkillType.Passive && Type != SkillType.Aura;
    public bool IsPassive => Type == SkillType.Passive;
    public bool IsAreaEffect => Type is SkillType.AreaOfEffect or SkillType.Aura;
}