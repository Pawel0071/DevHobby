namespace RPG.Domain.AI.Nodes.Dialogue.Conditions;

/// <summary>
///     Checks if player has completed a specific quest.
/// </summary>
public class HasCompletedQuestCondition : IDialogueNode
{
    private readonly Guid _questId;

    public HasCompletedQuestCondition(Guid questId)
    {
        _questId = questId;
    }

    public BehaviorStatus Execute(DialogueContext context)
    {
        return context.PlayerCompletedQuests.Contains(_questId)
            ? BehaviorStatus.Success
            : BehaviorStatus.Failure;
    }
}
