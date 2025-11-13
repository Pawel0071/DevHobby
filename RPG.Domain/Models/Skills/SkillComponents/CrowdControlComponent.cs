using RPG.Domain.Enums;

namespace RPG.Domain.Models.Skills.SkillComponents;

/// <summary>
///     Component for crowd control effects (stun, root, silence, etc.).
///     Pure data - CC application handled by services.
/// </summary>
public class CrowdControlComponent : ISkillComponent
{
    public CrowdControlType ControlType { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsDiminishingReturn { get; set; } = true; // Each successive CC is shorter
    public bool IsBreakableOnDamage { get; set; }
    public int BreakDamageThreshold { get; set; }
    public string ImmuneAfterBreak { get; set; } = string.Empty; // Buff ID for immunity
    public int ImmunityDurationSeconds { get; set; }
}
