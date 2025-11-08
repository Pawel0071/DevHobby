namespace RPG.Domain.AI.Nodes.Conditions;

/// <summary>
///     Checks if NPC has a valid target in range.
/// </summary>
public class HasTargetInRangeCondition : IBehaviorNode
{
    private readonly float _maxRange;

    public HasTargetInRangeCondition(float maxRange)
    {
        _maxRange = maxRange;
    }

    public BehaviorStatus Execute(AIContext context)
    {
        if (context.Target == null)
            return BehaviorStatus.Failure;

        if (context.DistanceToTarget <= _maxRange)
            return BehaviorStatus.Success;

        return BehaviorStatus.Failure;
    }
}
