namespace RPG.Domain.Models.Skills.SkillComponents;

/// <summary>
///     Component for skills that apply debuffs (negative effects).
///     Pure data - debuff application handled by services.
/// </summary>
public class DebuffComponent : ISkillComponent
{
    public string DebuffId { get; set; } = string.Empty;
    public Dictionary<string, int> StatModifiers { get; set; } = new(); // stat name -> penalty
    public Dictionary<string, float> StatMultipliers { get; set; } = new(); // stat name -> multiplier
    public int DurationSeconds { get; set; }
    public int MaxStacks { get; set; } = 1;
    public bool RefreshOnReapply { get; set; } = true;
    public bool IsCleansable { get; set; } = true;
    public string DebuffIcon { get; set; } = string.Empty;
    public string DebuffDescription { get; set; } = string.Empty;
}
