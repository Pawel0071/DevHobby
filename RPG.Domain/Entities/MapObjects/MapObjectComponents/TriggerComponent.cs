using RPG.Domain.Enums;

namespace RPG.Domain.Entities.MapObjects.MapObjectComponents;

/// <summary>
///     Component for trigger zones and event triggers.
///     Pure data - trigger logic handled by services.
/// </summary>
public class TriggerComponent : IMapObjectComponent
{
    public string TriggerEventId { get; set; } = string.Empty;
    public TriggerActivationType ActivationType { get; set; }
    public bool TriggerOnce { get; set; }
    public bool HasTriggered { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public int CooldownSeconds { get; set; }
    public float ProximityRadius { get; set; }
    public List<Guid> AllowedCharacterIds { get; set; } = new();
}
