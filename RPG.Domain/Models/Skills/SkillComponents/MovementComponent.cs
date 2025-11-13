using RPG.Domain.Enums;

namespace RPG.Domain.Models.Skills.SkillComponents;

/// <summary>
///     Component for movement effects (dash, teleport, knockback, etc.).
///     Pure data - movement logic handled by services.
/// </summary>
public class MovementComponent : ISkillComponent
{
    public MovementType MovementType { get; set; }
    public float Distance { get; set; }
    public float Speed { get; set; }
    public bool IgnoresObstacles { get; set; }
    public bool RequiresGroundTarget { get; set; }
    public bool CanMoveWhileCasting { get; set; }
    public bool PreventsMovement { get; set; } // For channeled skills
    public int ImmobilizeDurationSeconds { get; set; }
}
