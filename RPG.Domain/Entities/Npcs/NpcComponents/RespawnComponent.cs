namespace RPG.Domain.Entities.Npcs.NpcComponents;

/// <summary>
///     Component for NPCs that can respawn after death.
/// </summary>
public class RespawnComponent : INpcComponent
{
    public int RespawnTimeSeconds { get; set; } = 300; // 5 minutes default
    public DateTime? LastDeathTime { get; set; }
    public Location RespawnLocation { get; set; } = new();
}
