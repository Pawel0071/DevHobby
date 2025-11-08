namespace RPG.Domain.AI.Nodes.Dialogue.Conditions;

/// <summary>
///     Checks if player's reputation is at least the required amount.
/// </summary>
public class ReputationCondition : IDialogueNode
{
    private readonly int _requiredReputation;

    public ReputationCondition(int requiredReputation)
    {
        _requiredReputation = requiredReputation;
    }

    public BehaviorStatus Execute(DialogueContext context)
    {
        return context.PlayerReputation >= _requiredReputation
            ? BehaviorStatus.Success
            : BehaviorStatus.Failure;
    }
}
