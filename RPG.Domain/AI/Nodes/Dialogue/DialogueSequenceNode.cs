namespace RPG.Domain.AI.Nodes.Dialogue;

/// <summary>
///     Sequence node for dialogue trees.
///     Executes children in order until one fails.
/// </summary>
public class DialogueSequenceNode : IDialogueNode
{
    private readonly List<IDialogueNode> _children;
    private int _currentChildIndex;

    public DialogueSequenceNode(params IDialogueNode[] children)
    {
        _children = children.ToList();
    }

    public BehaviorStatus Execute(DialogueContext context)
    {
        while (_currentChildIndex < _children.Count)
        {
            var status = _children[_currentChildIndex].Execute(context);

            if (status == BehaviorStatus.Failure)
            {
                _currentChildIndex = 0;
                return BehaviorStatus.Failure;
            }

            if (status == BehaviorStatus.Running) return BehaviorStatus.Running;

            _currentChildIndex++;
        }

        _currentChildIndex = 0;
        return BehaviorStatus.Success;
    }
}
