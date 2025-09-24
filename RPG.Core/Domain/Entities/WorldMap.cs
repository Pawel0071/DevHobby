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
    public Location Position { get; set; } = new();
    public List<NpcCharacter> Npcs { get; set; } = new();
    public List<MonsterCharacter<DefaultMovementPattern>> Monsters { get; set; } = new();
    public List<StaticObject> StaticObjects { get; set; } = new();
    public List<Chest> Chests { get; set; } = new();
    public List<Waypoint> Waypoints { get; set; } = new();
    public List<Portal> Portals { get; set; } = new();
    public List<Trap> Traps { get; set; } = new();
}
