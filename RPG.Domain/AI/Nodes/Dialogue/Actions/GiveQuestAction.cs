namespace RPG.Domain.AI.Nodes.Dialogue.Actions;

/// <summary>
///     Gives a quest to the player.
///     Stores quest ID in blackboard for the service to process.
/// </summary>
public class GiveQuestAction : IDialogueNode
{
    private readonly Guid _questId;

    public GiveQuestAction(Guid questId)
    {
        _questId = questId;
    }

    public BehaviorStatus Execute(DialogueContext context)
    {
        context.SetBlackboardValue("QuestToGive", _questId);
        return BehaviorStatus.Success;
    }
}
