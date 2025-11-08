namespace RPG.Domain.AI.Nodes.Dialogue.Actions;

/// <summary>
///     Shows dialogue text to the player.
/// </summary>
public class ShowDialogueAction : IDialogueNode
{
    private readonly string _dialogueText;
    private readonly string? _nodeId;

    public ShowDialogueAction(string dialogueText, string? nodeId = null)
    {
        _dialogueText = dialogueText;
        _nodeId = nodeId;
    }

    public BehaviorStatus Execute(DialogueContext context)
    {
        context.SelectedDialogueText = _dialogueText;

        if (_nodeId != null)
        {
            context.CurrentDialogueNodeId = _nodeId;
            context.DialogueHistory.Add(_nodeId);
        }

        return BehaviorStatus.Success;
    }
}
