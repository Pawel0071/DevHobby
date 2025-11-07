using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;    

namespace RPG.Infrastructure.Outbox;

public class OutboxDispatcher : BackgroundService
{
    private readonly IMongoCollection<OutboxMessage> _outbox;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<OutboxDispatcher> _logger;
    private const int BatchSize = 10;
    private const int RetryDelaySeconds = 5;
    private const int MaxRetries = 3;

    public OutboxDispatcher(
        IMongoCollection<OutboxMessage> outbox,
        IRabbitMqPublisher publisher,
        ILogger<OutboxDispatcher> logger)
    {
        _outbox = outbox;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Info("OutboxDispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var unsent = await _outbox
                    .Find(x => !x.Sent && x.RetryCount < MaxRetries)
                    .Limit(BatchSize)
                    .ToListAsync(stoppingToken);

                foreach (var msg in unsent)
                {
                    await ProcessMessage(msg, stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.Error("Error in OutboxDispatcher main loop", ex);
                await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), stoppingToken);
            }
        }

        _logger.Warn("OutboxDispatcher stopped.");
    }

    private async Task ProcessMessage(OutboxMessage msg, CancellationToken stoppingToken)
    {
        try
        {
            _logger.Debug($"Dispatching message {msg.Id} to topic '{msg.Topic}'");
            await _publisher.PublishAsync(msg.Topic, msg.Payload);

            var update = Builders<OutboxMessage>.Update.Set(x => x.Sent, true);
            await _outbox.UpdateOneAsync(x => x.Id == msg.Id, update, cancellationToken: stoppingToken);

            _logger.Info($"Message {msg.Id} dispatched successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to dispatch message {msg.Id} (attempt {msg.RetryCount + 1}/{MaxRetries})", ex);
            
            var update = Builders<OutboxMessage>.Update
                .Inc(x => x.RetryCount, 1)
                .Set(x => x.LastRetryAt, DateTime.UtcNow);
            await _outbox.UpdateOneAsync(x => x.Id == msg.Id, update, cancellationToken: stoppingToken);
        }
    }
}