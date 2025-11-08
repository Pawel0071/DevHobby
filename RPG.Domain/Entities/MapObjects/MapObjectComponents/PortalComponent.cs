namespace RPG.Domain.Entities.MapObjects.MapObjectComponents;

/// <summary>
///     Component for portal/teleporter map objects.
///     Pure data - teleportation logic handled by services.
/// </summary>
public class PortalComponent : IMapObjectComponent
{
    public Guid DestinationWorldId { get; set; }
    public string DestinationZoneId { get; set; } = string.Empty;
    public Location DestinationLocation { get; set; } = new();
    public bool RequiresActivation { get; set; }
    public bool IsActivated { get; set; }
    public int MinimumLevel { get; set; }
    public List<Guid> RequiredQuestIds { get; set; } = new();
}
