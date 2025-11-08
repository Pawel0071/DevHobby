using RPG.Domain.Common;

namespace RPG.Domain.Entities.Npcs;

/// <summary>
///     Domain entity representing a Non-Player Character.
///     Uses tag-based and component-based system similar to Items.
///     Tags define what the NPC is (friendly, hostile, merchant, etc.)
///     Components define what the NPC can do (combat, dialogue, trading, etc.)
/// </summary>
public class Npc : IDomainEntity
{
    public static Npc Create(
        string name,
        string displayName,
        Location spawnLocation,
        Guid worldId,
        HashSet<string>? tags = null)
    {
        return new Npc
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = displayName,
            Description = string.Empty,
            SpawnLocation = spawnLocation,
            WorldId = worldId,
            Tags = tags ?? new HashSet<string>()
        };
    }

    private Npc()
    {
        Name = string.Empty;
        DisplayName = string.Empty;
        Description = string.Empty;
        SpawnLocation = new Location();
        Tags = new HashSet<string>();
        Components = new List<INpcComponent>();
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string DisplayName { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Level { get; set; }
    public Location SpawnLocation { get; private set; }
    public Guid WorldId { get; private set; }
    public HashSet<string> Tags { get; set; }
    public List<INpcComponent> Components { get; set; }
}
