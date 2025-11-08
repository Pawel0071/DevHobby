namespace RPG.Domain.AI.Nodes.Dialogue.Actions;

/// <summary>
///     Ends the conversation.
/// </summary>
public class EndConversationAction : IDialogueNode
{
    private readonly string _farewellText;

    public EndConversationAction(string farewellText = "Farewell, traveler.")
    {
        _farewellText = farewellText;
    }

    public BehaviorStatus Execute(DialogueContext context)
    {
        context.SelectedDialogueText = _farewellText;
        context.AvailableChoices.Clear();
        context.SetBlackboardValue("ConversationEnded", true);
        return BehaviorStatus.Success;
    }
}
