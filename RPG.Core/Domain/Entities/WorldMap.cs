using System.Numerics;
using RPG.Core.Domain.Entities.Common;

namespace RPG.Core.Domain.Entities;

public class WorldMap
{
    public string Name { get; set; } = string.Empty;
    public List<MapRegion> Regions { get; set; } = new();
}

public class MapRegion
{
    public string Name { get; set; } = string.Empty;
    public List<MapLocation> Locations { get; set; } = new();
}

public class MapLocation
{
    public string Name { get; set; } = string.Empty;
    public Vector3  Position { get; set; } = new();
}
