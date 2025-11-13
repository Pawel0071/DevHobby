using RPG.Domain.Enums;

namespace RPG.Domain.Models.Skills.SkillComponents;

/// <summary>
///     Component for area of effect settings.
///     Pure data - AoE targeting handled by services.
/// </summary>
public class AreaOfEffectComponent : ISkillComponent
{
    public AreaShape Shape { get; set; }
    public float Radius { get; set; }
    public float Width { get; set; } // For line/cone
    public float Length { get; set; } // For line/cone
    public int MaxTargets { get; set; }
    public bool AffectsAllies { get; set; }
    public bool AffectsEnemies { get; set; } = true;
    public bool AffectsSelf { get; set; }
    public bool RequiresLineOfSight { get; set; } = true;
}
