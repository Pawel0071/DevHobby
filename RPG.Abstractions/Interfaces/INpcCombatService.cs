using System;
using System.Threading;
using System.Threading.Tasks;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Skills;

namespace RPG.Abstractions.Interfaces;

/// <summary>
///     Coordinates combat-side effects when an NPC executes combat directives.
/// </summary>
public interface INpcCombatService
{
    /// <summary>
    ///     Handles a utility AI request for an NPC to use a skill against an optional target.
    /// </summary>
    Task HandleSkillUsageAsync(
        Npc npc,
        Skill skill,
        Guid? targetCharacterId,
        CancellationToken cancellationToken = default);
}
