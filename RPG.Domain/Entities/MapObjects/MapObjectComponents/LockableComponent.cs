namespace RPG.Domain.Entities.MapObjects.MapObjectComponents;

/// <summary>
///     Component for lockable map objects (doors, containers, gates).
///     Pure data - lock/unlock logic handled by services.
/// </summary>
public class LockableComponent : IMapObjectComponent
{
    public bool IsLocked { get; set; }
    public string? RequiredKeyItemId { get; set; }
    public int LockpickDifficulty { get; set; }
    public bool CanBeLockpicked { get; set; }
}
