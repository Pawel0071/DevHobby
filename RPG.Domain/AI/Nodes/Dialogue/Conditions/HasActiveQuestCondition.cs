namespace RPG.Domain.AI.Nodes.Dialogue.Conditions;

/// <summary>
///     Checks if player has an active quest.
/// </summary>
public class HasActiveQuestCondition : IDialogueNode
{
    private readonly Guid _questId;

    public HasActiveQuestCondition(Guid questId)
    {
        _questId = questId;
    }

    public BehaviorStatus Execute(DialogueContext context)
    {
        return context.PlayerActiveQuests.Contains(_questId)
            ? BehaviorStatus.Success
            : BehaviorStatus.Failure;
    }
}
