using System;
using RPG.AI.Core;

namespace RPG.AI.Utility.Considerations;

/// <summary>
///     Increases as the NPC falls outside of the preferred range while remaining inside the chase range.
/// </summary>
public sealed class DistanceGreaterThanConsideration : IUtilityConsideration
{
    private readonly float _desiredRange;
    private readonly float _chaseRange;

    public DistanceGreaterThanConsideration(string name, float desiredRange, float chaseRange)
    {
        if (desiredRange < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredRange));
        }

        if (chaseRange <= desiredRange)
        {
            throw new ArgumentOutOfRangeException(nameof(chaseRange));
        }

        Name = name;
        _desiredRange = desiredRange;
        _chaseRange = chaseRange;
    }

    public string Name { get; }

    public float Evaluate(AiContext context)
    {
        var distance = context.UpdateDistanceToTarget();
        if (float.IsPositiveInfinity(distance))
        {
            return 0f;
        }

        if (distance <= _desiredRange)
        {
            return 0f;
        }

        if (distance >= _chaseRange)
        {
            return 0f;
        }

        var range = _chaseRange - _desiredRange;
        var delta = distance - _desiredRange;
        return Math.Clamp(delta / range, 0f, 1f);
    }
}
