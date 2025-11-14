using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Broadcasters;

/// <summary>
/// Domyślna implementacja IGameStateBroadcaster oparta o logger.
/// Kolejny krok: integracja z gRPC streaming / WebSocket do klienta.
/// </summary>
public sealed class GameStateBroadcaster : IGameStateBroadcaster
{
    private readonly ILogger<GameStateBroadcaster> _logger;

    public GameStateBroadcaster(ILogger<GameStateBroadcaster> logger)
    {
        _logger = logger;
    }

    public Task BroadcastDeltaAsync(GameDeltaUpdate delta, CancellationToken cancellationToken = default)
    {
        if (delta == null)
        {
            _logger.Warn("Attempted to broadcast null game delta.");
            return Task.CompletedTask;
        }

        _logger.Debug($"Game delta for world={delta.WorldId} | npcs={delta.NpcChanges.Count} chars={delta.CharacterChanges.Count} mapObjects={delta.MapObjectChanges.Count} ts={delta.Timestamp:o}");
        return Task.CompletedTask;
    }
}

