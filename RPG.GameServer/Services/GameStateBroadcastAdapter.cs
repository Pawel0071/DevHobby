using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;

namespace RPG.GameServer.Services;

/// <summary>
/// Adapter IGameStateBroadcaster po stronie GameServera:
/// zamiast bezpośrednio wysyłać, buforuje delty w GameDeltaBuffer,
/// z którego korzysta WorldService.StreamWorldState (gRPC stream).
/// </summary>
public sealed class GameStateBroadcastAdapter : IGameStateBroadcaster
{
    private readonly GameDeltaBuffer _buffer;
    private readonly RPG.Infrastructure.Interfaces.ILogger<GameStateBroadcastAdapter> _logger;

    public GameStateBroadcastAdapter(GameDeltaBuffer buffer, RPG.Infrastructure.Interfaces.ILogger<GameStateBroadcastAdapter> logger)
    {
        _buffer = buffer;
        _logger = logger;
    }

    public Task BroadcastDeltaAsync(GameDeltaUpdate delta, CancellationToken cancellationToken = default)
    {
        if (delta == null)
        {
            return Task.CompletedTask;
        }

        _buffer.Enqueue(delta);
        _logger.Debug($"Enqueued game delta for world={delta.WorldId} | npc={delta.NpcChanges.Count} chars={delta.CharacterChanges.Count} map={delta.MapObjectChanges.Count}");
        return Task.CompletedTask;
    }
}
