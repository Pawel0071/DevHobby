using RPG.GameServer.Domain.Types;

namespace RPG.GameServer.Application.Combat;

public class CombatResult
{
    public bool IsSuccess { get; set; }
    public string SkillName { get; set; }
    public int DamageDealt { get; set; }
    public List<Effect> AppliedEffects { get; set; } = new();
    public bool TargetDefeated { get; set; }
    public string Message { get; set; }

    public static CombatResult Success(string skillName, int damage, List<Effect> effects, bool defeated)
    {
        return new CombatResult
        {
            IsSuccess = true,
            SkillName = skillName,
            DamageDealt = damage,
            AppliedEffects = effects,
            TargetDefeated = defeated,
            Message = defeated ? "Target defeated!" : "Skill executed successfully."
        };
    }

    public static CombatResult Failed(string reason)
    {
        return new CombatResult
        {
            IsSuccess = false,
            Message = reason
        };
    }
}