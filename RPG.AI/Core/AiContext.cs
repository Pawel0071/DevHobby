using System;
using System.Collections.Generic;
using System.Numerics;
using RPG.AI.Directives;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Npcs;

namespace RPG.AI.Core;

/// <summary>
///     Runtime snapshot passed to utility actions and considerations.
/// </summary>
public sealed class AiContext
{
    public Npc Self { get; set; } = null!;
    public Character? Target
    {
        get => _target;
        set
        {
            _target = value;
            DistanceToTarget = value is null ? float.PositiveInfinity : CalculateDistanceTo(value!);
        }
    }

    public List<Character> NearbyPlayers { get; } = new();
    public List<Npc> NearbyNpcs { get; } = new();

    // Vital statistics
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }

    public bool IsInCombat { get; set; }
    public DateTime? CombatStartTime { get; set; }

    public float DistanceToTarget { get; private set; } = float.PositiveInfinity;

    public Dictionary<Guid, DateTime> SkillCooldowns { get; } = new();

    public Dictionary<string, object> Blackboard { get; } = new();

    public List<AiDirective> Directives { get; } = new();

    public Dictionary<Guid, ThreatInfo> ThreatTable { get; } = new();

    public void Reset()
    {
        Target = null;
        NearbyPlayers.Clear();
        NearbyNpcs.Clear();
        Blackboard.Clear();
        Directives.Clear();
        SkillCooldowns.Clear();
        ThreatTable.Clear();
        IsInCombat = false;
        CombatStartTime = null;
        DistanceToTarget = float.PositiveInfinity;
    }

    public void SetBlackboardValue(string key, object value)
    {
        Blackboard[key] = value;
    }

    public bool TryGetBlackboardValue<T>(string key, out T? value)
    {
        if (Blackboard.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public void RemoveBlackboardValue(string key)
    {
        Blackboard.Remove(key);
    }

    public void IssueDirective(AiDirective directive)
    {
        if (directive == null)
        {
            throw new ArgumentNullException(nameof(directive));
        }

        Directives.Add(directive);
    }

    public float CalculateDistanceTo(Character character)
    {
        if (Self?.CurrentLocation?.Position is not { } npcPosition)
        {
            return float.PositiveInfinity;
        }

        var targetPosition = character?.CurrentLocation?.Position ?? Vector3.Zero;
        return Vector3.Distance(npcPosition, targetPosition);
    }

    public float CalculateDistanceTo(Location? location)
    {
        if (Self?.CurrentLocation?.Position is not { } npcPosition)
        {
            return float.PositiveInfinity;
        }

        var destination = location?.Position ?? Vector3.Zero;
        return Vector3.Distance(npcPosition, destination);
    }

    public float UpdateDistanceToTarget()
    {
        if (Target is null)
        {
            DistanceToTarget = float.PositiveInfinity;
            return DistanceToTarget;
        }

    DistanceToTarget = CalculateDistanceTo(Target!);
        return DistanceToTarget;
    }

    private Character? _target;
}

public sealed record ThreatInfo(Guid CharacterId, float Score, float Distance, DateTime LastSeenUtc);
