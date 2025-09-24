namespace RPG.Core.Domain.Entities.Common;

public class Waypoint
{
    public string Name { get; set; } = "Waypoint";
    public Location Position { get; set; } = new();
    public bool IsActivated { get; set; } = false;
}