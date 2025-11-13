using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RPG.Abstractions.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace RPG.Application.Infrastructure;

internal sealed class EventQueueHostedService : BackgroundService
{
    private readonly IEventMessageQueue _queue;
    private readonly ILogger<EventQueueHostedService> _logger;
    private const int DelayMs = 50;

    public EventQueueHostedService(IEventMessageQueue queue, ILogger<EventQueueHostedService> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _queue.PublishAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing events from queue");
            }
            await Task.Delay(DelayMs, stoppingToken);
        }
    }
}
