namespace RPG.Domain.Models.Skills.SkillComponents;

/// <summary>
///     Component for skills that heal.
///     Pure data - healing calculation handled by services.
/// </summary>
public class HealingComponent : ISkillComponent
{
    public int BaseHealing { get; set; }
    public float ScalingFactor { get; set; }
    public string ScalingStat { get; set; } = "intelligence"; // Which stat scales healing
    public string HealingType { get; set; } = "direct"; // direct, over-time, shield
    public bool CanCrit { get; set; } = true;
    public float CritMultiplier { get; set; } = 1.5f;
    public int MinHealing { get; set; }
    public int MaxHealing { get; set; }
    public bool AffectsSelf { get; set; } = true;
    public bool AffectsAllies { get; set; } = true;
}
