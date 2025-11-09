using System;
using RPG.AI.Core;

namespace RPG.AI.Utility.Considerations;

/// <summary>
///     Evaluates whether a skill is off cooldown. Returns 1 when ready and decays while the cooldown elapses.
/// </summary>
public sealed class CooldownConsideration : IUtilityConsideration
{
    private readonly Guid _skillId;
    private readonly TimeSpan _cooldown;

    public CooldownConsideration(string name, Guid skillId, TimeSpan cooldown)
    {
        Name = name;
        _skillId = skillId;
        _cooldown = cooldown;
    }

    public string Name { get; }

    public float Evaluate(AiContext context)
    {
        if (!context.SkillCooldowns.TryGetValue(_skillId, out var readyAt))
        {
            return 1f;
        }

        var now = DateTime.UtcNow;
        if (readyAt <= now)
        {
            return 1f;
        }

        var remaining = (float)(readyAt - now).TotalSeconds;
        var total = (float)_cooldown.TotalSeconds;
        if (total <= 0f)
        {
            return 1f;
        }

        return Math.Clamp(1f - (remaining / total), 0f, 1f);
    }
}
