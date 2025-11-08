using RPG.Domain.Entities;
using RPG.Domain.Entities.Npcs;

namespace RPG.Domain.AI;

/// <summary>
///     Context passed to behavior nodes during execution.
///     Contains all information needed for AI decision making.
/// </summary>
public class AIContext
{
    public Npc Self { get; set; } = null!;
    public Character? Target { get; set; }
    public List<Character> NearbyPlayers { get; set; } = new();
    public List<Npc> NearbyNpcs { get; set; } = new();

    // NPC State (set by AI service before tree execution)
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }

    // Combat state
    public float DistanceToTarget { get; set; }
    public bool IsInCombat { get; set; }
    public DateTime? CombatStartTime { get; set; }

    // Cooldowns
    public Dictionary<Guid, DateTime> SkillCooldowns { get; set; } = new();

    // Blackboard - shared data between nodes
    public Dictionary<string, object> Blackboard { get; set; } = new();

    public void SetBlackboardValue(string key, object value)
    {
        Blackboard[key] = value;
    }

    public T? GetBlackboardValue<T>(string key)
    {
        return Blackboard.TryGetValue(key, out var value) && value is T typedValue ? typedValue : default;
    }
}
