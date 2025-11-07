using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.RabbitMQ;

/// <summary>
/// Null Object Pattern - używany gdy RabbitMQ nie jest skonfigurowany.
/// Pozwala aplikacji działać bez RabbitMQ dla dev/test.
/// </summary>
public class NullRabbitPublisher : IRabbitPublisher
{
    public Task PublishAsync<T>(string topic, T message)
    {
        // Do nothing - silent no-op
        return Task.CompletedTask;
    }
}
