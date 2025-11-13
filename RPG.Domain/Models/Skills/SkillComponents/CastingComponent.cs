namespace RPG.Domain.Models.Skills.SkillComponents;

/// <summary>
///     Component for skill casting requirements and behavior.
///     Pure data - casting logic handled by services.
/// </summary>
public class CastingComponent : ISkillComponent
{
    public int CastTimeMs { get; set; }
    public bool IsChanneled { get; set; }
    public int ChannelDurationMs { get; set; }
    public int ChannelTickIntervalMs { get; set; }
    public bool CanMoveWhileCasting { get; set; }
    public bool IsInterruptible { get; set; } = true;
    public bool RequiresTarget { get; set; }
    public bool RequiresLineOfSight { get; set; } = true;
    public float MaxRange { get; set; }
    public float MinRange { get; set; }
    public string CastAnimation { get; set; } = string.Empty;
}
