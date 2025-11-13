using RPG.Domain.Common;

namespace RPG.Domain.Models.MapObjects;

/// <summary>
///     Domain entity representing an interactive object in the game world.
///     Uses tags for categorization and components for capabilities.
///     Pure data entity - logic handled by services.
/// </summary>
public class MapObject : IDomainModel
{
    private MapObject()
    {
        Name = string.Empty;
        DisplayName = string.Empty;
        Description = string.Empty;
        ZoneId = string.Empty;
        Location = new Location();
        Tags = new HashSet<string>();
        Components = new List<IMapObjectComponent>();
        State = new Dictionary<string, string>();
        LastUpdated = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }

    // Location & Positioning
    public Location Location { get; set; }
    public float RotationYaw { get; set; }
    public Guid WorldId { get; set; }
    public string ZoneId { get; set; }

    // State
    public bool IsActive { get; set; }

    // Tags for categorization
    public HashSet<string> Tags { get; set; }

    // Components for capabilities
    public List<IMapObjectComponent> Components { get; set; }

    // Arbitrary map object state (e.g., lock status, resource counts)
    public Dictionary<string, string> State { get; set; }

    // Timestamp for last update in world state context
    public DateTime LastUpdated { get; set; }

    public static MapObject Create(
        string name,
        Location location,
        Guid worldId,
        string zoneId = "")
    {
        return new MapObject
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = name,
            Description = string.Empty,
            Location = location,
            WorldId = worldId,
            ZoneId = zoneId,
            RotationYaw = 0f,
            IsActive = true
        };
    }

    // Helper method to get component of specific type
    public T? GetComponent<T>() where T : class, IMapObjectComponent
    {
        return Components.OfType<T>().FirstOrDefault();
    }
}
