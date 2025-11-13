namespace RPG.Domain.Models.Skills.SkillComponents;

/// <summary>
///     Component for skills that apply buffs (positive effects).
///     Pure data - buff application handled by services.
/// </summary>
public class BuffComponent : ISkillComponent
{
    public string BuffId { get; set; } = string.Empty;
    public Dictionary<string, int> StatModifiers { get; set; } = new(); // stat name -> value
    public Dictionary<string, float> StatMultipliers { get; set; } = new(); // stat name -> multiplier
    public int DurationSeconds { get; set; }
    public bool IsPermanent { get; set; }
    public int MaxStacks { get; set; } = 1;
    public bool RefreshOnReapply { get; set; } = true;
    public bool IsDispellable { get; set; } = true;
    public string BuffIcon { get; set; } = string.Empty;
    public string BuffDescription { get; set; } = string.Empty;
}
