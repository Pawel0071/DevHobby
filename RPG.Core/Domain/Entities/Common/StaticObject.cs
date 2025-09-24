namespace RPG.Core.Domain.Entities.Common;

public class StaticObject
{
    public string Name { get; set; } = string.Empty;
    public Location Position { get; set; } = new();
}