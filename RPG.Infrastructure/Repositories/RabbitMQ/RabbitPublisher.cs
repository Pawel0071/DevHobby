using RPG.Infrastructure.Configuration;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.RabbitMQ;

public class RabbitPublisher : IRabbitPublisher
{
    private readonly IChannel _channel;
    private readonly ILogger<RabbitPublisher> _logger;
    private readonly string _exchangeName;

    public RabbitPublisher(IChannel channel, ILogger<RabbitPublisher> logger, RabbitMqSettings settings)
    {
        _channel = channel;
        _logger = logger;
        _exchangeName = settings.ExchangeName;
    }

    public async Task PublishAsync<T>(string topic, T message)
    {
        try
        {
            var json = JsonConvert.SerializeObject(message);
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