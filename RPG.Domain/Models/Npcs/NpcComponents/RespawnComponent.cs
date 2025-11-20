namespace RPG.Domain.Models.Npcs.NpcComponents;

/// <summary>
///     Component for NPCs that can respawn after death.
/// </summary>
public class RespawnComponent : NpcComponentBase
{
    public override string ComponentName => "Respawn";
    public override string ComponentType => "Respawn";

    public int RespawnTimeSeconds { get; set; } = 300; // 5 minutes default
    public DateTime? LastDeathTime { get; set; }
    public Location RespawnLocation { get; set; } = new();
}
