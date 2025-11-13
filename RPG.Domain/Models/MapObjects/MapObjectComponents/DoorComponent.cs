namespace RPG.Domain.Models.MapObjects.MapObjectComponents;

/// <summary>
///     Component for door-like map objects (doors, gates, portcullises).
///     Pure data - open/close logic handled by services.
/// </summary>
public class DoorComponent : IMapObjectComponent
{
    public bool IsOpen { get; set; }
    public Guid? LinkedDoorId { get; set; }
    public string? OpenAnimation { get; set; }
    public string? CloseAnimation { get; set; }
    public float OpenAngle { get; set; }
    public bool AutoClose { get; set; }
    public int AutoCloseDelaySeconds { get; set; }
}
