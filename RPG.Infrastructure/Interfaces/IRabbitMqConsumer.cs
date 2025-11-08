namespace RPG.Infrastructure.Interfaces;

/// <summary>
///     Consumer for RabbitMQ messages that processes generic documents
/// </summary>
public interface IRabbitMqConsumer
{
    /// <summary>
    ///     Sets the message handler callback
    /// </summary>
    void SetMessageHandler(Func<string, string, CancellationToken, Task> handler);

    /// <summary>
    ///     Starts consuming messages from RabbitMQ asynchronously
    /// </summary>
    Task StartConsumingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stops consuming messages
    /// </summary>
    Task StopConsumingAsync();
}
