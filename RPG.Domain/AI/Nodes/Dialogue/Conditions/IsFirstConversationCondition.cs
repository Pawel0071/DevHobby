namespace RPG.Domain.AI.Nodes.Dialogue.Conditions;

/// <summary>
///     Checks if this is the first conversation with the NPC.
/// </summary>
public class IsFirstConversationCondition : IDialogueNode
{
    public BehaviorStatus Execute(DialogueContext context)
    {
        return context.ConversationTurn == 0
            ? BehaviorStatus.Success
            : BehaviorStatus.Failure;
    }
}
