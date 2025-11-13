using RPG.Domain.Common;

namespace RPG.Domain.Models;

/// <summary>
///     Domain entity representing world state.
///     Pure data entity - logic handled by services.
/// </summary>
public class WorldState : IDomainModel
{
    private WorldState()
    {
        WorldName = string.Empty;
        Characters = new List<Guid>();
        Npcs = new List<Guid>();
        MapObjects = new List<Guid>();
    }

    public Guid Id { get; private set; }
    // WorldId is an alias of Id to keep backward compatibility in code using WorldId
    public Guid WorldId => Id;
    public string WorldName { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<Guid> Characters { get; }
    public List<Guid> Npcs { get; }
    public List<Guid> MapObjects { get; }

    public static WorldState Create(Guid worldId, string worldName)
    {
        return new WorldState
        {
            Id = worldId,
            WorldName = worldName,
            LastUpdated = DateTime.UtcNow
        };
    }

    public static WorldState Hydrate(
        Guid id,
        Guid worldId,
        string worldName,
        DateTime lastUpdated,
        IEnumerable<Guid>? characters = null,
        IEnumerable<Guid>? npcs = null,
        IEnumerable<Guid>? mapObjects = null)
    {
        var worldState = new WorldState
        {
            // prefer worldId as canonical identifier
            Id = worldId,
            WorldName = worldName,
            LastUpdated = lastUpdated
        };

        if (characters != null)
        {
            worldState.Characters.AddRange(characters);
        }

        if (npcs != null)
        {
            worldState.Npcs.AddRange(npcs);
        }

        if (mapObjects != null)
        {
            worldState.MapObjects.AddRange(mapObjects);
        }

        return worldState;
    }
}
