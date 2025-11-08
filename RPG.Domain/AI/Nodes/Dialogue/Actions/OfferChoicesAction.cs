namespace RPG.Domain.AI.Nodes.Dialogue.Actions;

/// <summary>
///     Offers dialogue choices to the player.
/// </summary>
public class OfferChoicesAction : IDialogueNode
{
    private readonly List<DialogueChoice> _choices;

    public OfferChoicesAction(params DialogueChoice[] choices)
    {
        _choices = choices.ToList();
    }

    public BehaviorStatus Execute(DialogueContext context)
    {
        context.AvailableChoices = _choices;
        return BehaviorStatus.Success;
    }
}
