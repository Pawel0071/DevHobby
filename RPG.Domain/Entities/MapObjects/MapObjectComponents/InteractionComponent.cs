namespace RPG.Domain.Entities.MapObjects.MapObjectComponents;

/// <summary>
///     Component for interaction settings.
///     Pure data - interaction logic handled by services.
/// </summary>
public class InteractionComponent : IMapObjectComponent
{
    public bool IsInteractable { get; set; } = true;
    public float InteractionRadius { get; set; } = 3.0f;
    public string InteractionPrompt { get; set; } = "Press E to interact";
    public int InteractionDurationMs { get; set; }
    public bool RequiresLineOfSight { get; set; } = true;
    public int MaxSimultaneousUsers { get; set; } = 1;
    public int CooldownSeconds { get; set; }
}
