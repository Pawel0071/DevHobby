namespace RPG.Domain.Models.Skills.SkillComponents;

/// <summary>
///     Component for healing over time effects (HoT).
///     Pure data - HoT application handled by services.
/// </summary>
public class HealOverTimeComponent : ISkillComponent
{
    public int HealingPerTick { get; set; }
    public int TickIntervalSeconds { get; set; } = 3;
    public int DurationSeconds { get; set; }
    public float ScalingFactor { get; set; }
    public string ScalingStat { get; set; } = "intelligence";
    public int MaxStacks { get; set; } = 1;
    public bool CanCrit { get; set; } = false;
    public bool AffectsSelf { get; set; } = true;
    public bool AffectsAllies { get; set; } = true;
    public string EffectIcon { get; set; } = string.Empty;
}
