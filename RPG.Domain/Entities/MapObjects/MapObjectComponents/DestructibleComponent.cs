namespace RPG.Domain.Entities.MapObjects.MapObjectComponents;

/// <summary>
///     Component for destructible map objects.
///     Pure data - damage/destruction logic handled by services.
/// </summary>
public class DestructibleComponent : IMapObjectComponent
{
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public bool IsDestroyed { get; set; }
    public int ArmorRating { get; set; }
    public List<string> VulnerableToDamageTypes { get; set; } = new();
    public List<string> ImmuneToDamageTypes { get; set; } = new();
    public Guid? DestroyedModelId { get; set; }
}
