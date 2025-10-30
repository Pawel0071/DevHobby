namespace RPG.Infrastructure.Interfaces;

public interface IRabbitPublisher
{
    Task PublishAsync<T>(string topic, T message);
}