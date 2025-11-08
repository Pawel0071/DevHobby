namespace RPG.Domain.AI.Nodes.Dialogue;

/// <summary>
///     Selector node for dialogue trees.
///     Tries children until one succeeds.
/// </summary>
public class DialogueSelectorNode : IDialogueNode
{
    private readonly List<IDialogueNode> _children;
    private int _currentChildIndex;

    public DialogueSelectorNode(params IDialogueNode[] children)
    {
        _children = children.ToList();
    }

    public BehaviorStatus Execute(DialogueContext context)
    {
        while (_currentChildIndex < _children.Count)
        {
            var status = _children[_currentChildIndex].Execute(context);

            if (status == BehaviorStatus.Success)
            {
                _currentChildIndex = 0;
                return BehaviorStatus.Success;
            }

            if (status == BehaviorStatus.Running) return BehaviorStatus.Running;

            _currentChildIndex++;
        }

        _currentChildIndex = 0;
        return BehaviorStatus.Failure;
    }
}
