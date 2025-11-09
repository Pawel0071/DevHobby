using System;
using RPG.AI.Core;

namespace RPG.AI.Utility.Considerations;

/// <summary>
///     Scores higher as the NPC gets closer to the target.
/// </summary>
public sealed class DistanceToTargetConsideration : IUtilityConsideration
{
    private readonly float _optimalDistance;
    private readonly float _maxDistance;

    public DistanceToTargetConsideration(string name, float optimalDistance, float maxDistance)
    {
        if (maxDistance <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDistance));
        }

        if (optimalDistance < 0f || optimalDistance > maxDistance)
        {
            throw new ArgumentOutOfRangeException(nameof(optimalDistance));
        }

        Name = name;
        _optimalDistance = optimalDistance;
        _maxDistance = maxDistance;
    }

    public string Name { get; }

    public float Evaluate(AiContext context)
    {
        var distance = context.UpdateDistanceToTarget();

        if (float.IsPositiveInfinity(distance))
        {
            return 0f;
        }

        if (distance <= _optimalDistance)
        {
            return 1f;
        }

        if (distance >= _maxDistance)
        {
            return 0f;
        }

        var range = _maxDistance - _optimalDistance;
        var delta = Math.Clamp(distance - _optimalDistance, 0f, range);
        return 1f - (delta / range);
    }
}
