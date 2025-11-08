namespace RPG.Domain.AI.Nodes.Conditions;

/// <summary>
///     Checks if NPC's health is below a threshold percentage.
/// </summary>
public class HealthBelowCondition : IBehaviorNode
{
    private readonly float _percentage;

    public HealthBelowCondition(float percentage)
    {
        _percentage = percentage;
    }

    public BehaviorStatus Execute(AIContext context)
    {
        if (context.MaxHealth == 0)
            return BehaviorStatus.Failure;

        var healthPercent = (float)context.CurrentHealth / context.MaxHealth;

        return healthPercent < _percentage
            ? BehaviorStatus.Success
            : BehaviorStatus.Failure;
    }
}
