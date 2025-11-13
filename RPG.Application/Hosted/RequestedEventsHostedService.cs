using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RPG.Application.Infrastructure;
using RPG.Application.Interfaces;
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RequestedEventsHostedService> _logger;

    public RequestedEventsHostedService(
        IRequestEventQueue requestQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<RequestedEventsHostedService> logger)
    {
        _requestQueue = requestQueue;
        _scopeFactory = scopeFactory;
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
                    using var scope = _scopeFactory.CreateScope();
                    var handlers = scope.ServiceProvider.GetRequiredService<IEnumerable<IRequestedEventHandler>>();
                    var matched = false;
                    foreach (var h in handlers)
                    {
                        if (!h.CanHandle(evt)) continue;
                        matched = true;
                        await h.HandleAsync(evt, stoppingToken);
                        break;
                    }
                    if (!matched)
                    {
                        _logger.Warn($"No IRequestedEventHandler matched event type: {evt.GetType().Name}");
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
