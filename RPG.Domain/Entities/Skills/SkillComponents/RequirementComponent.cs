namespace RPG.Domain.Entities.Skills.SkillComponents;

/// <summary>
///     Component for skill requirements (level, class, weapon, etc.).
///     Pure data - requirement validation handled by services.
/// </summary>
public class RequirementComponent : ISkillComponent
{
    public int RequiredLevel { get; set; }
    public List<string> RequiredClasses { get; set; } = new();
    public List<string> RequiredWeaponTypes { get; set; } = new();
    public bool RequiresMeleeWeapon { get; set; }
    public bool RequiresRangedWeapon { get; set; }
    public Dictionary<string, int> RequiredStats { get; set; } = new(); // stat -> minimum value
    public List<Guid> RequiredSkillIds { get; set; } = new(); // Prerequisites
    public List<Guid> RequiredBuffIds { get; set; } = new(); // Must have certain buffs
    public List<string> ForbiddenBuffIds { get; set; } = new(); // Cannot have certain buffs
}
