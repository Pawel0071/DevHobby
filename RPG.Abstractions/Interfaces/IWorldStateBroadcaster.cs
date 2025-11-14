using RPG.Domain.Models;

namespace RPG.Abstractions.Interfaces;

/// <summary>
/// Broadcastuje lekkie zmiany stanu świata (WorldState) do warstwy prezentacji/klienta.
/// Operuje tylko na identyfikatorach/licznikach, bez pełnych modeli NPC/MapObject.
/// </summary>
public interface IWorldStateBroadcaster
{
    Task BroadcastWorldStateAsync(WorldState worldState, CancellationToken cancellationToken = default);
}

