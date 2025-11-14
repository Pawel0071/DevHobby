using RPG.Abstractions.SharedModel;

namespace RPG.Abstractions.Interfaces;

/// <summary>
/// Broadcastuje delty stanu gry (NPC/Characters/MapObjects) do klienta.
/// </summary>
public interface IGameStateBroadcaster
{
    Task BroadcastDeltaAsync(GameDeltaUpdate delta, CancellationToken cancellationToken = default);
}

