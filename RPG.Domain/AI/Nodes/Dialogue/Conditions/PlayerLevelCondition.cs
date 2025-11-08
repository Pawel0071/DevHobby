namespace RPG.Domain.AI.Nodes.Dialogue.Conditions;

/// <summary>
///     Checks if player's level is at least the required level.
/// </summary>
public class PlayerLevelCondition : IDialogueNode
{
    private readonly int _requiredLevel;

    public PlayerLevelCondition(int requiredLevel)
    {
        _requiredLevel = requiredLevel;
    }

    public BehaviorStatus Execute(DialogueContext context)
    {
        return context.PlayerLevel >= _requiredLevel
            ? BehaviorStatus.Success
            : BehaviorStatus.Failure;
    }
}
