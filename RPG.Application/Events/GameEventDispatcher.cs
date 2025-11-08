using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RPG.Application.Interfaces;

namespace RPG.Application.Events;

public class GameEventDispatcher : IGameEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GameEventDispatcher> _logger;

    public GameEventDispatcher(IServiceProvider serviceProvider, ILogger<GameEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync<TEvent>(TEvent gameEvent, CancellationToken cancellationToken = default)
    {
        if (gameEvent == null)
        {
            _logger.LogWarning("Skipping null game event of type {EventType}", typeof(TEvent).Name);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IGameEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(gameEvent, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Dispatch of {EventType} cancelled.", typeof(TEvent).Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling game event {EventType}", typeof(TEvent).Name);
            }
        }
    }
}
