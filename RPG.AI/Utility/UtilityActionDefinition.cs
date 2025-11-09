using System;
using System.Collections.Generic;
using System.Linq;
using RPG.AI.Core;
using RPG.AI.Directives;

namespace RPG.AI.Utility;

/// <summary>
///     Declarative description of a utility AI action.
/// </summary>
public sealed class UtilityActionDefinition
{
    private readonly Func<AiContext, IEnumerable<AiDirective>> _executor;
    private readonly IReadOnlyList<IUtilityConsideration> _considerations;

    public UtilityActionDefinition(
        string name,
        Func<AiContext, IEnumerable<AiDirective>> executor,
        IEnumerable<IUtilityConsideration>? considerations = null,
        float weight = 1f,
        Func<AiContext, bool>? predicate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Action name must be provided.", nameof(name));
        }

        Name = name;
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _considerations = (considerations ?? Array.Empty<IUtilityConsideration>()).ToArray();
        Weight = Math.Clamp(weight, 0f, 10f);
        Predicate = predicate;
    }

    public string Name { get; }

    public float Weight { get; }

    public Func<AiContext, bool>? Predicate { get; }

    public UtilityEvaluation Evaluate(AiContext context)
    {
        if (Predicate != null && !Predicate(context))
        {
            return UtilityEvaluation.Blocked(this);
        }

        if (_considerations.Count == 0)
        {
            return UtilityEvaluation.Ready(this, Weight);
        }

        var score = Weight;
        foreach (var consideration in _considerations)
        {
            var value = Math.Clamp(consideration.Evaluate(context), 0f, 1f);
            score *= value;
            if (score <= 0f)
            {
                return UtilityEvaluation.Unfavorable(this);
            }
        }

        return UtilityEvaluation.Ready(this, score);
    }

    public IEnumerable<AiDirective> Execute(AiContext context)
    {
        return _executor(context);
    }

    public IReadOnlyList<IUtilityConsideration> Considerations => _considerations;
}

public readonly record struct UtilityEvaluation(UtilityActionDefinition Action, float Score, bool IsBlocked)
{
    public static UtilityEvaluation Ready(UtilityActionDefinition action, float score)
    {
        return new UtilityEvaluation(action, score, false);
    }

    public static UtilityEvaluation Blocked(UtilityActionDefinition action)
    {
        return new UtilityEvaluation(action, 0f, true);
    }

    public static UtilityEvaluation Unfavorable(UtilityActionDefinition action)
    {
        return new UtilityEvaluation(action, 0f, false);
    }
}
