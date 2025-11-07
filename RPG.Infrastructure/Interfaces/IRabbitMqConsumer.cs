namespace RPG.Infrastructure.Interfaces;

/// <summary>
/// Consumer for RabbitMQ messages that processes generic documents
/// </summary>
public interface IRabbitMqConsumer
{
    /// <summary>
    /// Starts consuming messages from RabbitMQ asynchronously
    /// </summary>
    Task StartConsumingAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops consuming messages
    /// </summary>
    Task StopConsumingAsync();
}
