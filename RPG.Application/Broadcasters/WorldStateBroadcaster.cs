using RPG.Abstractions.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Broadcasters;

/// <summary>
/// Prosta implementacja broadcastu WorldState oparta o logger.
/// Docelowo można ją zastąpić mechanizmem push do klienta (SignalR/gRPC streaming).
/// </summary>
public class WorldStateBroadcaster : IWorldStateBroadcaster
{
    private readonly ILogger<WorldStateBroadcaster> _logger;

    public WorldStateBroadcaster(ILogger<WorldStateBroadcaster> logger)
    {
        _logger = logger;
    }

    public Task BroadcastWorldStateAsync(WorldState worldState, CancellationToken cancellationToken = default)
    {
        if (worldState == null)
        {
            _logger.Warn("Attempted to broadcast null WorldState.");
            return Task.CompletedTask;
        }

        _logger.Debug($"Broadcast WorldState {worldState.WorldId} | chars={worldState.Characters.Count} npcs={worldState.Npcs.Count} mapObjects={worldState.MapObjects.Count} lastUpdated={worldState.LastUpdated:o}");

        // W tej chwili broadcast to tylko log. Kolejny krok: wysyłka przez EventBroadcaster/IEventBroadcaster do klienckiego kanału.
        return Task.CompletedTask;
    }
}

