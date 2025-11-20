using RPG.Domain.Containers;
using RPG.Domain.Enums;
using RPG.Domain.Models.Skills;
using RPG.Domain.Models.Npcs.NpcComponents;

namespace RPG.Domain.Models.Npcs.NpcComponents;

/// <summary>
///     Component for NPCs that can engage in combat.
///     Defines combat stats, skills and battle behavior/AI.
/// </summary>
public class CombatComponent : NpcComponentBase
{
    public override string ComponentName => "Combat";
    public override string ComponentType => "Combat";

    // Combat AI behavior
    public float AggroRange { get; set; }
    public float LeashRange { get; set; }
    public string AiBehaviorScript { get; set; } = string.Empty; // Script name or AI type
}
