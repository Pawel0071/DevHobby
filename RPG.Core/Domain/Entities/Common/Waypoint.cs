using System.Numerics;

namespace RPG.Core.Domain.Entities.Common;

public class Waypoint
{
    public string Name { get; set; } = "Waypoint";
    public Vector3 Position { get; set; } = new();
    public bool IsActivated { get; set; } = false;
}