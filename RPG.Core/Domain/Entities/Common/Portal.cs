using System.Numerics;

namespace RPG.Core.Domain.Entities.Common;

public class Portal
{
    public string Name { get; set; } = "Portal";
    public Vector3 Position { get; set; } = new();
    public string Destination { get; set; } = string.Empty;
}