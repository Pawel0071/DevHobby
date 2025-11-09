using RPG.AI.Core;

namespace RPG.AI.Utility.Considerations;

public sealed class HasTargetConsideration : IUtilityConsideration
{
    public HasTargetConsideration(string name = "has-target")
    {
        Name = name;
    }

    public string Name { get; }

    public float Evaluate(AiContext context)
    {
        return context.Target is null ? 0f : 1f;
    }
}
