using Microsoft.Extensions.Logging;
using RPG.Infrastructure.Interfaces;

namespace RPG.PersistenceService.Service;

public class RabbitMqToMongoService : IRabbitMqToMongoService
{
    private readonly IRabbitMqConsumer _rabbitConsumer;
    private readonly Microsoft.Extensions.Logging.ILogger<RabbitMqToMongoService> _logger;

    public RabbitMqToMongoService(
        IRabbitMqConsumer rabbitConsumer,
        Microsoft.Extensions.Logging.ILogger<RabbitMqToMongoService> logger)
    {
        _rabbitConsumer = rabbitConsumer;
        _logger = logger;
    }

    public async Task StartListeningAsync()
    {
        _logger.LogInformation("Starting RabbitMQ to MongoDB service");
        await _rabbitConsumer.StartConsumingAsync();
    }
}