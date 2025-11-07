namespace RPG.Infrastructure.Interfaces;

/// <summary>
/// Publisher for sending messages to RabbitMQ exchange.
/// </summary>
public interface IRabbitMqPublisher
{
    /// <summary>
    /// Publishes a message to the specified topic (routing key).
    /// </summary>
    /// <typeparam name="T">Type of message to publish</typeparam>
    /// <param name="topic">Routing key for the message</param>
    /// <param name="message">Message object to publish</param>
    Task PublishAsync<T>(string topic, T message);
}