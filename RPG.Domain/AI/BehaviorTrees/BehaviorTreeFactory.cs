using RPG.Domain.AI.Nodes;
using RPG.Domain.AI.Nodes.Actions;
using RPG.Domain.AI.Nodes.Conditions;
using RPG.Domain.Entities.Skills;

namespace RPG.Domain.AI.BehaviorTrees;

/// <summary>
///     Factory for creating predefined AI behavior trees.
///     These can be referenced by name in CombatComponent.AiBehaviorScript.
/// </summary>
public static class BehaviorTreeFactory
{
    /// <summary>
    ///     Aggressive melee NPC that rushes and attacks on sight.
    /// </summary>
    public static IBehaviorNode CreateAggressiveMelee(Skill basicAttack)
    {
        return new SelectorNode(
            // If no target, find one
            new SequenceNode(
                new InverterNode(new HasTargetInRangeCondition(100f)),
                new SelectNearestTargetAction()
            ),

            // If target exists, attack
            new SequenceNode(
                new HasTargetInRangeCondition(5f), // Melee range
                new SkillAvailableCondition(basicAttack),
                new UseSkillAction(basicAttack)
            )
        );
    }

    /// <summary>
    ///     Defensive NPC that heals when low health.
    /// </summary>
    public static IBehaviorNode CreateDefensiveHealer(Skill heal, Skill basicAttack)
    {
        return new SelectorNode(
            // Priority 1: Heal if low health
            new SequenceNode(
                new HealthBelowCondition(0.3f), // Below 30% HP
                new SkillAvailableCondition(heal),
                new UseSkillAction(heal)
            ),

            // Priority 2: Attack if target in range
            new SequenceNode(
                new HasTargetInRangeCondition(5f),
                new SkillAvailableCondition(basicAttack),
                new UseSkillAction(basicAttack)
            ),

            // Priority 3: Find target
            new SelectNearestTargetAction()
        );
    }

    /// <summary>
    ///     Caster NPC that uses multiple spells based on cooldowns.
    /// </summary>
    public static IBehaviorNode CreateCaster(Skill fireball, Skill frostbolt, Skill basicAttack)
    {
        return new SelectorNode(
            // Find target if needed
            new SequenceNode(
                new InverterNode(new HasTargetInRangeCondition(30f)),
                new SelectNearestTargetAction()
            ),

            // Use fireball if available (highest damage)
            new SequenceNode(
                new HasTargetInRangeCondition(30f),
                new SkillAvailableCondition(fireball),
                new UseSkillAction(fireball)
            ),

            // Use frostbolt if fireball on cooldown
            new SequenceNode(
                new HasTargetInRangeCondition(30f),
                new SkillAvailableCondition(frostbolt),
                new UseSkillAction(frostbolt)
            ),

            // Fall back to basic attack
            new SequenceNode(
                new HasTargetInRangeCondition(30f),
                new SkillAvailableCondition(basicAttack),
                new UseSkillAction(basicAttack)
            )
        );
    }

    /// <summary>
    ///     Boss NPC with complex rotation based on health phases.
    /// </summary>
    public static IBehaviorNode CreateBoss(
        Skill ultimateSkill,
        Skill powerAttack,
        Skill basicAttack)
    {
        return new SelectorNode(
            // Phase 1: Below 20% HP - spam ultimate
            new SequenceNode(
                new HealthBelowCondition(0.2f),
                new HasTargetInRangeCondition(15f),
                new SkillAvailableCondition(ultimateSkill),
                new UseSkillAction(ultimateSkill)
            ),

            // Phase 2: Use power attack when available
            new SequenceNode(
                new HasTargetInRangeCondition(10f),
                new SkillAvailableCondition(powerAttack),
                new UseSkillAction(powerAttack)
            ),

            // Phase 3: Basic rotation
            new SequenceNode(
                new HasTargetInRangeCondition(10f),
                new SkillAvailableCondition(basicAttack),
                new UseSkillAction(basicAttack)
            ),

            // Find target
            new SelectNearestTargetAction()
        );
    }

    /// <summary>
    ///     Get behavior tree by name (used in CombatComponent.AiBehaviorScript).
    /// </summary>
    public static IBehaviorNode? GetByName(string scriptName, Dictionary<string, Skill> skills)
    {
        return scriptName.ToLower() switch
        {
            "aggressive-melee" => CreateAggressiveMelee(
                skills.GetValueOrDefault("basic-attack")!),

            "defensive-healer" => CreateDefensiveHealer(
                skills.GetValueOrDefault("heal")!,
                skills.GetValueOrDefault("basic-attack")!),

            "caster" => CreateCaster(
                skills.GetValueOrDefault("fireball")!,
                skills.GetValueOrDefault("frostbolt")!,
                skills.GetValueOrDefault("basic-attack")!),

            "boss" => CreateBoss(
                skills.GetValueOrDefault("ultimate")!,
                skills.GetValueOrDefault("power-attack")!,
                skills.GetValueOrDefault("basic-attack")!),

            _ => null
        };
    }
}
