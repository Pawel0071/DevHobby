using System;
using RPG.AI.Core;

namespace RPG.AI.Utility.Considerations;

/// <summary>
///     Returns lower scores when health falls below the configured threshold.
/// </summary>
public sealed class HealthRatioConsideration : IUtilityConsideration
{
    private readonly float _threshold;
    private readonly bool _invert;

    public HealthRatioConsideration(string name, float threshold, bool invert = false)
    {
        if (threshold < 0f || threshold > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold));
        }

        Name = name;
        _threshold = threshold;
        _invert = invert;
    }

    public string Name { get; }

    public float Evaluate(AiContext context)
    {
        if (context.MaxHealth <= 0)
        {
            return 0f;
        }

        var ratio = Math.Clamp((float)context.CurrentHealth / context.MaxHealth, 0f, 1f);

        if (!_invert)
        {
            return ratio >= _threshold ? 1f : ratio / (_threshold <= 0f ? 1f : _threshold);
        }

        return ratio <= _threshold ? 1f : Math.Clamp(1f - ((ratio - _threshold) / (1f - _threshold)), 0f, 1f);
    }
}
