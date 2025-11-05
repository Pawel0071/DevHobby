using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;    

namespace RPG.Infrastructure.Outbox;

public class OutboxDispatcher : BackgroundService
{
    private readonly IMongoCollection<OutboxMessage> _outbox;
    private readonly IRabbitPublisher _publisher;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        IMongoCollection<OutboxMessage> outbox,
        IRabbitPublisher publisher,
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
            var unsent = await _outbox.Find(x => !x.Sent).Limit(10).ToListAsync(stoppingToken);

            foreach (var msg in unsent)
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
                    _logger.Error($"Failed to dispatch message {msg.Id}", ex);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.Warn("OutboxDispatcher stopped.");
    }
}