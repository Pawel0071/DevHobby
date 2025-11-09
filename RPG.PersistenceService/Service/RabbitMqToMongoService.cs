using System.Threading;
using RPG.Infrastructure.Interfaces;
using RPG.PersistenceService.Handlers;

namespace RPG.PersistenceService.Service;

public class RabbitMqToMongoService : IRabbitMqToMongoService
{
    private readonly Infrastructure.Interfaces.ILogger<RabbitMqToMongoService> _logger;
    private readonly IRabbitMqConsumer _rabbitConsumer;
    private readonly MessageHandler _messageHandler;

    public RabbitMqToMongoService(
        IRabbitMqConsumer rabbitConsumer,
        MessageHandler messageHandler,
        Infrastructure.Interfaces.ILogger<RabbitMqToMongoService> logger)
    {
        _rabbitConsumer = rabbitConsumer;
        _messageHandler = messageHandler;
        _logger = logger;
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Starting RabbitMQ to MongoDB service");

        // Set message handler
        _rabbitConsumer.SetMessageHandler(_messageHandler.HandleMessageAsync);

        // Start consuming
        await _rabbitConsumer.StartConsumingAsync(cancellationToken);

        _logger.Info("RabbitMQ consumer started");
    }

    public async Task StopListeningAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Stopping RabbitMQ to MongoDB service");

        await _rabbitConsumer.StopConsumingAsync();

        _logger.Info("RabbitMQ consumer stopped");
    }
}
