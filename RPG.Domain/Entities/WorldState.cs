using RPG.Domain.Common;

namespace RPG.Domain.Entities;

/// <summary>
///     Domain entity representing world state.
///     Pure data entity - logic handled by services.
/// </summary>
public class WorldState : IDomainEntity
{
    private WorldState()
    {
        WorldName = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid WorldId { get; private set; }
    public string WorldName { get; set; }
    public DateTime LastUpdated { get; set; }

    public static WorldState Create(Guid worldId, string worldName)
    {
        return new WorldState
        {
            Id = Guid.NewGuid(), WorldId = worldId, WorldName = worldName, LastUpdated = DateTime.UtcNow
        };
    }
}
