using RPG.Infrastructure.Configuration;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.RabbitMQ;

/// <summary>
/// RabbitMQ publisher for sending messages to exchange.
/// </summary>
public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly IChannel _channel;
    private readonly Interfaces.ILogger<RabbitMqPublisher> _logger;
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly string _routingKey;

    public RabbitMqPublisher(
        IChannel channel, 
        Interfaces.ILogger<RabbitMqPublisher> logger, 
        RabbitMqSettings settings)
    {
        _channel = channel;
        _logger = logger;
        _exchangeName = settings.ExchangeName;
        _queueName = settings.QueueName ?? "rpg_persistence_queue";
        _routingKey = settings.RoutingKey ?? "#";
        
        _logger.Info($"RabbitMqPublisher initialized: Exchange={_exchangeName}, Queue={_queueName}, RoutingKey={_routingKey}");
    }

    public async Task PublishAsync<T>(string topic, T message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _logger.Debug($"Publishing message to topic '{topic}': {json}");

            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: topic,
                mandatory: false,
                body: body
            );

            _logger.Info($"Message published to topic '{topic}'");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to publish message to topic '{topic}'", ex);
            throw;
        }
    }
}