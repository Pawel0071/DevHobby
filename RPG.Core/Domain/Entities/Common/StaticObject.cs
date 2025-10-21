using System.Numerics;

namespace RPG.Core.Domain.Entities.Common;

public class StaticObject
{
    public string Name { get; set; } = string.Empty;
    public Vector3 Position { get; set; } = new();
}