using RPG.Domain.Entities.Skills;

namespace RPG.Domain.AI.Nodes.Conditions;

/// <summary>
///     Checks if a specific skill is off cooldown.
/// </summary>
public class SkillAvailableCondition : IBehaviorNode
{
    private readonly Skill _skill;

    public SkillAvailableCondition(Skill skill)
    {
        _skill = skill;
    }

    public BehaviorStatus Execute(AIContext context)
    {
        if (!context.SkillCooldowns.TryGetValue(_skill.Id, out var cooldownEnd))
            return BehaviorStatus.Success;

        return DateTime.UtcNow >= cooldownEnd
            ? BehaviorStatus.Success
            : BehaviorStatus.Failure;
    }
}
