using RPG.Domain.Models;

namespace RPG.Abstractions.SharedModel;

/// <summary>
/// Lekka delta zmian w świecie gry pomiędzy tickami/heartbeatami.
/// Klient używa jej do aktualizacji lokalnego WorldState, nie pobierając pełnych modeli.
/// </summary>
public sealed class GameDeltaUpdate
{
    public Guid WorldId { get; init; }

    // NPC, których stan/pozycja się zmieniła od ostatniego broadcastu
    public IReadOnlyList<NpcDelta> NpcChanges { get; init; } = Array.Empty<NpcDelta>();

    // Inni gracze (postacie), których stan się zmienił
    public IReadOnlyList<CharacterDelta> CharacterChanges { get; init; } = Array.Empty<CharacterDelta>();

    // MapObject, których stan (np. drzwi otwarte/zamknięte) się zmienił
    public IReadOnlyList<MapObjectDelta> MapObjectChanges { get; init; } = Array.Empty<MapObjectDelta>();

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public sealed class NpcDelta
{
    public Guid NpcId { get; init; }
    public Location? Location { get; init; }
    public bool? IsAlive { get; init; }
}

public sealed class CharacterDelta
{
    public Guid CharacterId { get; init; }
    public Location? Location { get; init; }
    public bool? IsOnline { get; init; }
}

public sealed class MapObjectDelta
{
    public Guid MapObjectId { get; init; }
    public Location? Location { get; init; }
    public bool? IsActive { get; init; }
}
