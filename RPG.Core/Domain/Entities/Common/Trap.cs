namespace RPG.Core.Domain.Entities.Common;

public class Trap
{
    public string Name { get; set; } = "Trap";
    public Location Position { get; set; } = new();
    public int Damage { get; set; } = 0;
    public bool IsTriggered { get; set; } = false;
}