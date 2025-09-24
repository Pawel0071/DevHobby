namespace RPG.Core.Domain.Entities.Common;

public class Portal
{
    public string Name { get; set; } = "Portal";
    public Location Position { get; set; } = new();
    public string Destination { get; set; } = string.Empty;
}