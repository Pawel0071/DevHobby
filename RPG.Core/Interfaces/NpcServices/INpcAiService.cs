using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPG.AI.Models;
using RPG.Domain.Models;
using RPG.Domain.Models.Interaction;

namespace RPG.Core.Interfaces.NpcServices;

public interface INpcAiService
{
    Task<IReadOnlyList<AiEvaluationResult>> TickAsync(CancellationToken cancellationToken = default);
    IReadOnlyCollection<NpcStateSnapshot> GetNpcSnapshots();
    IReadOnlyCollection<AiEvaluationResult> GetLastEvaluations();
    void RegisterExternalThreat(Guid npcId, Guid characterId, float threatAmount, float? distance = null);
}
