namespace RPG.Domain.Entities.Skills.SkillComponents;

/// <summary>
///     Component for damage over time effects (DoT).
///     Pure data - DoT application handled by services.
/// </summary>
public class DamageOverTimeComponent : ISkillComponent
{
    public int DamagePerTick { get; set; }
    public int TickIntervalSeconds { get; set; } = 3;
    public int DurationSeconds { get; set; }
    public string DamageType { get; set; } = "physical";
    public float ScalingFactor { get; set; }
    public string ScalingStat { get; set; } = "intelligence";
    public int MaxStacks { get; set; } = 1;
    public bool CanCrit { get; set; } = false;
    public string EffectIcon { get; set; } = string.Empty;
}
