using RPG.Domain.Containers;
using RPG.Domain.Enums;
using RPG.Domain.Models.Skills;

namespace RPG.Domain.Models.Npcs.NpcComponents;

/// <summary>
///     Component for NPCs that can engage in combat.
///     Defines combat stats, skills and battle behavior/AI.
/// </summary>
public class CombatComponent : INpcComponent
{
    private StatsContainer StatsContainer { get; } = new();
    public IDictionary<StatsProperty, int> Stats => StatsContainer.Stats;

    private SkillsContainer SkillsContainer { get; } = new();
    public IDictionary<Skill, SkillAvailability> Skills => SkillsContainer.Skills;

    // Combat AI behavior
    public float AggroRange { get; set; }
    public float LeashRange { get; set; }
    public string AiBehaviorScript { get; set; } = string.Empty; // Script name or AI type

    public StatsContainer GetStatsContainer()
    {
        return StatsContainer;
    }

    public SkillsContainer GetSkillsContainer()
    {
        return SkillsContainer;
    }
}
