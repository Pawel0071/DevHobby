using RPG.Domain.Entities.Skills;

namespace RPG.Domain.AI.Nodes.Actions;

/// <summary>
///     Action node that uses a specific skill on the target.
///     This is a placeholder - actual skill execution would be handled by a service.
/// </summary>
public class UseSkillAction : IBehaviorNode
{
    private readonly Skill _skill;

    public UseSkillAction(Skill skill)
    {
        _skill = skill;
    }

    public BehaviorStatus Execute(AIContext context)
    {
        if (context.Target == null)
            return BehaviorStatus.Failure;

        // Store the skill to be used in blackboard
        // Actual execution will be handled by combat service
        context.SetBlackboardValue("SkillToUse", _skill);
        context.SetBlackboardValue("SkillTarget", context.Target);

        // Record skill usage in cooldowns (actual cooldown time would come from skill component)
        // This is a placeholder - real implementation would get cooldown from CooldownComponent
        context.SkillCooldowns[_skill.Id] = DateTime.UtcNow.AddSeconds(10); // Default 10s cooldown

        return BehaviorStatus.Success;
    }
}
