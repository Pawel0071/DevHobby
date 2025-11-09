using System;
using System.Collections.Generic;
using System.Linq;
using RPG.AI.Core;
using RPG.AI.Directives;

namespace RPG.AI.Utility;

/// <summary>
///     Evaluates registered utility actions and emits directives for the highest-scoring action.
/// </summary>
public sealed class UtilityAgent
{
    private readonly List<UtilityActionDefinition> _actions = new();

    public UtilityAgent(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public UtilityAgent Register(UtilityActionDefinition action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        _actions.Add(action);
        return this;
    }

    public UtilityDecision Decide(AiContext context)
    {
        if (_actions.Count == 0)
        {
            return UtilityDecision.Empty;
        }

        UtilityEvaluation? best = null;
        foreach (var evaluation in _actions.Select(action => action.Evaluate(context)))
        {
            if (evaluation.IsBlocked)
            {
                continue;
            }

            if (evaluation.Score <= 0f)
            {
                continue;
            }

            if (best is null || evaluation.Score > best.Value.Score)
            {
                best = evaluation;
            }
        }

        if (best is null)
        {
            return UtilityDecision.Empty;
        }

        var directives = best.Value.Action.Execute(context).ToArray();
        foreach (var directive in directives)
        {
            context.IssueDirective(directive);
        }

        return new UtilityDecision(best.Value.Action, best.Value.Score, directives);
    }

    public IReadOnlyList<UtilityActionDefinition> Actions => _actions;
}

public sealed record UtilityDecision(UtilityActionDefinition? Action, float Score, IReadOnlyList<AiDirective> Directives)
{
    public static readonly UtilityDecision Empty = new(null, 0f, Array.Empty<AiDirective>());

    public bool HasAction => Action != null;
}
