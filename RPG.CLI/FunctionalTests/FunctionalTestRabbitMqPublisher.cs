using System.Text.Json;
using RPG.Infrastructure.Interfaces;
using RPG.PersistenceService.Handlers;

namespace RPG.CLI.FunctionalTests;

/// <summary>
///     Test publisher that short-circuits RabbitMQ by passing messages directly to the persistence handler.
/// </summary>
internal sealed class FunctionalTestRabbitMqPublisher : IRabbitMqPublisher
{
    private readonly MessageHandler _messageHandler;
    private readonly ILogger<FunctionalTestRabbitMqPublisher> _logger;

    public FunctionalTestRabbitMqPublisher(MessageHandler messageHandler, ILogger<FunctionalTestRabbitMqPublisher> logger)
    {
        _messageHandler = messageHandler;
        _logger = logger;
    }

    public async Task PublishAsync<T>(string topic, T message)
    {
        var payload = JsonSerializer.Serialize(message);
        _logger.Debug($"[FunctionalTest] Publishing message to topic '{topic}' with payload length {payload.Length}");
        await _messageHandler.HandleMessageAsync(payload, topic, CancellationToken.None);
    }
}
