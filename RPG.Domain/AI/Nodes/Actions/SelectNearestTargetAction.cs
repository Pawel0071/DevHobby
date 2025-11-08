namespace RPG.Domain.AI.Nodes.Actions;

/// <summary>
///     Action that makes NPC select the nearest player as target.
/// </summary>
public class SelectNearestTargetAction : IBehaviorNode
{
    public BehaviorStatus Execute(AIContext context)
    {
        if (context.NearbyPlayers.Count == 0)
            return BehaviorStatus.Failure;

        // For now, just select first player
        // In real implementation, would calculate distance to each
        context.Target = context.NearbyPlayers[0];

        return BehaviorStatus.Success;
    }
}
