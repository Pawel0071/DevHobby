using RPG.AI.Core;

namespace RPG.AI.Utility.Considerations;

public sealed class NoTargetConsideration : IUtilityConsideration
{
    public NoTargetConsideration(string name = "no-target")
    {
        Name = name;
    }

    public string Name { get; }

    public float Evaluate(AiContext context)
    {
        return context.Target is null ? 1f : 0f;
    }
}
