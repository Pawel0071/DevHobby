using RPG.Domain.Models;

namespace RPG.Domain.Interfaces;

public interface IMovable : IStats
{
    Guid Id { get; }
    string Name { get; }
    public Location SpawnLocation { get; set; }
    public Location CurrentLocation { get; set; }
    public bool IsMoving { get; set; }
    public bool IsRotating { get; set; }
    public Guid? WorldId => CurrentLocation.WorldId;
}
