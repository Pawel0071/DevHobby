namespace RPG.Domain.AI.Nodes;

/// <summary>
///     Composite node that executes children until one succeeds.
///     Returns Success if any child succeeds.
///     Returns Failure if all children fail.
/// </summary>
public class SelectorNode : IBehaviorNode
{
    private readonly List<IBehaviorNode> _children;
    private int _currentChildIndex;

    public SelectorNode(params IBehaviorNode[] children)
    {
        _children = children.ToList();
    }

    public BehaviorStatus Execute(AIContext context)
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
