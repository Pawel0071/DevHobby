namespace RPG.Domain.Entities.Skills.SkillComponents;

/// <summary>
///     Component for skill cooldown settings.
///     Pure data - cooldown tracking handled by services.
/// </summary>
public class CooldownComponent : ISkillComponent
{
    public int CooldownSeconds { get; set; }
    public bool UseGlobalCooldown { get; set; } = true;
    public int GlobalCooldownMs { get; set; } = 1500;
    public int MaxCharges { get; set; } = 1; // For multi-charge abilities
    public int ChargeRecoverySeconds { get; set; }
    public bool SharesCooldownWith { get; set; }
    public List<Guid> SharedCooldownSkillIds { get; set; } = new();
}
