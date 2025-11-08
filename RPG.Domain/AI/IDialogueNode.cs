namespace RPG.Domain.AI;

/// <summary>
///     Base interface for dialogue behavior tree nodes.
///     Similar to IBehaviorNode but for dialogue systems.
/// </summary>
public interface IDialogueNode
{
    /// <summary>
    ///     Execute the dialogue node logic.
    /// </summary>
    /// <param name="context">Dialogue context containing player and conversation state</param>
    /// <returns>Status of the node execution</returns>
    BehaviorStatus Execute(DialogueContext context);
}
