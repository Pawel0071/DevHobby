namespace RPG.Domain.Models.Npcs;

/// <summary>
///     Base interface for all NPC components.
///     Components define specific behaviors and capabilities of NPCs.
/// </summary>
public interface INpcComponent
{
    Guid OwnerId { get; }
    Npc? Owner { get; }
    bool IsAttached { get; }
    string ComponentName { get; }
    string ComponentType { get; }
    void Attach(Npc owner);
    void Detach();
    void Tick(TimeSpan deltaTime);
}
