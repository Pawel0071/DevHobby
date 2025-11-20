using RPG.Domain.Models.Npcs.NpcComponents;

namespace RPG.Domain.Models.Npcs.NpcComponents;

/// <summary>
///     Defines AI behavior profile for this NPC.
/// </summary>
public class AiComponent : NpcComponentBase
{
    public override string ComponentName => "AI";
    public override string ComponentType => "AI";

    /// <summary>
    ///     AI profile name (e.g., "Aggressive", "Passive", "PatrolGuard", "Merchant").
    /// </summary>
    public string Profile { get; set; } = "Passive";

    /// <summary>
    ///     Detection range in world units.
    /// </summary>
    public float DetectionRange { get; set; } = 10f;

    /// <summary>
    ///     Aggro range - distance at which NPC will engage combat.
    /// </summary>
    public float AggroRange { get; set; } = 5f;

    /// <summary>
    ///     Leash range - maximum distance from spawn before returning.
    /// </summary>
    public float LeashRange { get; set; } = 30f;

    /// <summary>
    ///     AI update tick interval in milliseconds.
    /// </summary>
    public int TickIntervalMs { get; set; } = 1000;

    /// <summary>
    ///     Patrol configuration (if patrol behavior is enabled).
    /// </summary>
    public PatrolConfig? Patrol { get; set; }

    /// <summary>
    ///     Additional AI parameters as key-value pairs.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class PatrolConfig
{
    /// <summary>
    ///     Patrol radius around spawn point.
    /// </summary>
    public float Radius { get; set; } = 10f;

    /// <summary>
    ///     Number of waypoints in patrol route.
    /// </summary>
    public int WaypointCount { get; set; } = 3;

    /// <summary>
    ///     Time to wait at each waypoint (in seconds).
    /// </summary>
    public float DwellTimeSeconds { get; set; } = 2f;

    /// <summary>
    ///     Distance from waypoint to consider it reached.
    /// </summary>
    public float StopDistance { get; set; } = 0.5f;
}
