namespace RPG.Domain.Entities.Skills.SkillComponents;

/// <summary>
///     Component for skills that deal damage.
///     Pure data - damage calculation handled by services.
/// </summary>
public class DamageComponent : ISkillComponent
{
    public int BaseDamage { get; set; }
    public float ScalingFactor { get; set; } // Multiplier for stat scaling
    public string ScalingStat { get; set; } = "strength"; // Which stat scales damage
    public string DamageType { get; set; } = "physical"; // physical, fire, ice, etc.
    public bool CanCrit { get; set; } = true;
    public float CritMultiplier { get; set; } = 2.0f;
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
}
