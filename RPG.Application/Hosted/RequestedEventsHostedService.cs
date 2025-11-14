using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RPG.Abstractions.Interfaces;
using RPG.Application.Infrastructure;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Hosted;

/// <summary>
/// Single hosted service that pulls events from IRequestEventQueue and dispatches
/// them to registered IRequestedEventHandler implementations.
/// Resolves handlers from a scope per event to respect Scoped lifetime.
/// </summary>
public sealed class RequestedEventsHostedService : BackgroundService
{
    private readonly IRequestEventQueue _requestQueue;
    private readonly IRequestedEventOrchestrator _orchestrator;
    private readonly ILogger<RequestedEventsHostedService> _logger;

    public RequestedEventsHostedService(
        IRequestEventQueue requestQueue,
        IRequestedEventOrchestrator orchestrator,
        ILogger<RequestedEventsHostedService> logger)
    {
        _requestQueue = requestQueue;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Info("RequestedEventsHostedService started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_requestQueue.TryDequeue(out var evt))
                {
                    if (evt is IGameEventWithMetadata metaEvt)
                    {
                        var handled = await _orchestrator.TryHandleAsync(metaEvt, stoppingToken);
                        if (!handled)
                        {
                            _logger.Warn($"No IRequestedEventHandler matched event type: {evt.GetType().Name}");
                        }
                    }
                    else
                    {
                        _logger.Warn($"Dequeued event without metadata: {evt.GetType().Name}");
                    }
                }
                else
                {
                    await Task.Delay(25, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.Error("Error in RequestedEventsHostedService loop", ex);
                await Task.Delay(200, stoppingToken);
            }
        }
        _logger.Info("RequestedEventsHostedService stopped");
    }
}
