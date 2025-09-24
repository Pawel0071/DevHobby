using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;

namespace RPG.Core.Domain.Entities;

public class Skill
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public SkillType Type { get; set; }
    public int ManaCost { get; set; }
    public float CooldownSeconds { get; set; }
    public int RequiredLevel { get; set; }
    public int Power { get; set; } // np. obrażenia, leczenie, siła buffa
    public float Range { get; set; } // zasięg działania
    public float DurationSeconds { get; set; } // czas trwania efektu
    public bool IsPassive => Type == SkillType.Passive;
    public bool IsAreaEffect => Type is SkillType.AreaOfEffect or SkillType.Aura;
    public string? EffectDescription { get; set; }
    public List<Effect> AppliedEffects { get; set; } = [];
    public bool RequiresTarget => Type != SkillType.Passive && Type != SkillType.Aura;
    public bool CanCastWhileMoving { get; set; } = false;
}