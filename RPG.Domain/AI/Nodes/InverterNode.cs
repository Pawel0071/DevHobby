namespace RPG.Domain.AI.Nodes;

/// <summary>
///     Decorator node that inverts the result of its child.
///     Success becomes Failure and vice versa.
/// </summary>
public class InverterNode : IBehaviorNode
{
    private readonly IBehaviorNode _child;

    public InverterNode(IBehaviorNode child)
    {
        _child = child;
    }

    public BehaviorStatus Execute(AIContext context)
    {
        var status = _child.Execute(context);

        return status switch
        {
            BehaviorStatus.Success => BehaviorStatus.Failure,
            BehaviorStatus.Failure => BehaviorStatus.Success,
            _ => status
        };
    }
}
