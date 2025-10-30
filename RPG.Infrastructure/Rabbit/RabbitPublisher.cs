using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Rabbit;

public class RabbitPublisher : IRabbitPublisher
{
    private readonly IChannel _channel;

    public RabbitPublisher(IChannel channel)
    {
        _channel = channel;
    }

    public async Task PublishAsync<T>(string topic, T message)
    {
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
        await _channel.BasicPublishAsync(
            exchange: "items",
            routingKey: topic,
            mandatory: false,
            body: body
        );
    }
}