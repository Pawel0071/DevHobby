using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.RabbitMQ;

/// <summary>
/// Null Object Pattern - używany gdy RabbitMQ nie jest skonfigurowany.
/// Pozwala aplikacji działać bez RabbitMQ dla dev/test.
/// </summary>
public class NullRabbitMqPublisher : IRabbitMqPublisher
{
    private readonly ILogger<NullRabbitMqPublisher>? _logger;

    public NullRabbitMqPublisher(ILogger<NullRabbitMqPublisher>? logger = null)
    {
        _logger = logger;
        _logger?.Info("NullRabbitMqPublisher initialized - RabbitMQ messages will not be published");
    }

    public Task PublishAsync<T>(string topic, T message)
    {
        _logger?.Debug($"NullRabbitMqPublisher: Skipping message publish to topic={topic}");
        // Do nothing - silent no-op
        return Task.CompletedTask;
    }
}
