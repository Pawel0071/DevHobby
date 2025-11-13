using System.Collections.Generic;
using RPG.AI.Core;
using RPG.AI.Directives;
using RPG.AI.Utility;
using RPG.Domain.Models.Npcs;

namespace RPG.AI.Models;

public sealed record AiEvaluationResult(
    Npc Npc,
    UtilityAgent Agent,
    AiContext Context,
    UtilityDecision Decision,
    IReadOnlyList<AiDirective> Directives,
    IReadOnlyList<string> ExecutionLog)
{
    public bool HasDirectives => Directives.Count > 0;
}
