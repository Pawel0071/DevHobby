using System.Numerics;

namespace RPG.Domain.Models;

/// <summary>
///     Value object representing a location in the game world.
///     Pure data - distance calculations handled by services.
/// </summary>
public class Location
{
    public Location()
    {
        Position = Vector3.Zero;
    }

    public Vector3 Position { get; set; }
    public Guid? WorldId { get; set; }
    public string MapId { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public float Rotation { get; set; } // Yaw rotation in degrees (0-360)

    public static Location Create(Vector3 position, Guid worldId, string mapId = "", string zoneName = "")
    {
        return new Location { Position = position, WorldId = worldId, MapId = mapId, ZoneName = zoneName };
    }

    public static Location Create(float x, float y, float z, Guid worldId, string mapId = "", string zoneName = "")
    {
        return new Location { Position = new Vector3(x, y, z), WorldId = worldId, MapId = mapId, ZoneName = zoneName };
    }
}
