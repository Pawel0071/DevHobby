namespace RPG.Domain.AI.Nodes;

/// <summary>
///     Composite node that executes children in sequence until one fails.
///     Returns Success if all children succeed.
///     Returns Failure if any child fails.
/// </summary>
public class SequenceNode : IBehaviorNode
{
    private readonly List<IBehaviorNode> _children;
    private int _currentChildIndex;

    public SequenceNode(params IBehaviorNode[] children)
    {
        _children = children.ToList();
    }

    public BehaviorStatus Execute(AIContext context)
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
