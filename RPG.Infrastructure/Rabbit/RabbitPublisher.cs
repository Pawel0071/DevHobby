using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Rabbit;

public class RabbitPublisher : IRabbitPublisher
{
    private readonly IChannel _channel;
    private readonly ILogger<RabbitPublisher> _logger;

    public RabbitPublisher(IChannel channel, ILogger<RabbitPublisher> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    public async Task PublishAsync<T>(string topic, T message)
    {
        try
        {
            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            _logger.Debug($"Publishing message to topic '{topic}': {json}");

            await _channel.BasicPublishAsync(
                exchange: "items",
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