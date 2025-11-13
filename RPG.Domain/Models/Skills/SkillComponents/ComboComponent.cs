namespace RPG.Domain.Models.Skills.SkillComponents;

/// <summary>
///     Component for combo/chain skills.
///     Pure data - combo tracking handled by services.
/// </summary>
public class ComboComponent : ISkillComponent
{
    public List<Guid> ComboSkillIds { get; set; } = new(); // Skills that can follow this one
    public int ComboWindowSeconds { get; set; } = 5; // Time window to execute next skill
    public List<Guid> RequiresPreviousSkills { get; set; } = new(); // Must be used after these skills
    public int ComboStage { get; set; } // Which stage in the combo chain
    public bool ResetsCombo { get; set; } // If this skill breaks the combo
    public Dictionary<string, float> ComboBonuses { get; set; } = new(); // stat -> bonus multiplier
}
