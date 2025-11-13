namespace RPG.Domain.Models.Skills.SkillComponents;

/// <summary>
///     Component for shield/absorption effects.
///     Pure data - shield application handled by services.
/// </summary>
public class ShieldComponent : ISkillComponent
{
    public int ShieldAmount { get; set; }
    public float ScalingFactor { get; set; }
    public string ScalingStat { get; set; } = "intelligence";
    public int DurationSeconds { get; set; }
    public List<string> AbsorbsDamageTypes { get; set; } = new(); // Empty = all types
    public bool IsPercentageBased { get; set; } // If true, ShieldAmount is % of max health
    public int MaxAbsorbAmount { get; set; }
    public string ShieldIcon { get; set; } = string.Empty;
}
