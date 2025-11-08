namespace RPG.Domain.AI;

/// <summary>
///     Base interface for all behavior tree nodes.
/// </summary>
public interface IBehaviorNode
{
    /// <summary>
    ///     Execute the node logic.
    /// </summary>
    /// <param name="context">AI context containing NPC, target, and world state</param>
    /// <returns>Status of the node execution</returns>
    BehaviorStatus Execute(AIContext context);
}

/// <summary>
///     Result status of behavior node execution.
/// </summary>
public enum BehaviorStatus
{
    Success, // Node completed successfully
    Failure, // Node failed to complete
    Running // Node is still executing (async/multi-frame)
}
