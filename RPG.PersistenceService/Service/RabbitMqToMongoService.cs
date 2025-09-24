using System.Text;
using System.Text.Json;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RPG.Core.Domain.Entities;


namespace RPG.PersistanceService.Infrastructure;

public class RabbitMqToMongoService : IRabbitMqToMongoService
{
    private readonly IMongoCollection<PlayerCharacter> _mongoCollection;
    private readonly IModel _rabbitChannel;
    private const string ExchangeName = "rpg_exchange";
    private const string QueueName = "rpg_queue";

    public RabbitMqToMongoService(IMongoDatabase mongoDatabase, IConnection rabbitConnection)
    {
        _mongoCollection = mongoDatabase.GetCollection<PlayerCharacter>("Characters");
        _rabbitChannel = rabbitConnection.CreateModel();

        _rabbitChannel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
        _rabbitChannel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        _rabbitChannel.QueueBind(QueueName, ExchangeName, routingKey: "character.*");
    }

    public void StartListening()
    {
        var consumer = new EventingBasicConsumer(_rabbitChannel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            await HandleMessage(routingKey, message);
        };

        _rabbitChannel.BasicConsume(queue: QueueName, autoAck: true, consumer: consumer);
    }

    private async Task HandleMessage(string routingKey, string message)
    {
        var character = JsonSerializer.Deserialize<PlayerCharacter>(message);
        if (character == null) return;

        switch (routingKey)
        {
            case "character.created":
                await _mongoCollection.InsertOneAsync(character);
                break;

            case "character.updated":
                var filter = Builders<PlayerCharacter>.Filter.Eq(c => c.Id, character.Id);
                await _mongoCollection.ReplaceOneAsync(filter, character, new ReplaceOptions { IsUpsert = true });
                break;

            case "character.deleted":
                var deleteFilter = Builders<PlayerCharacter>.Filter.Eq(c => c.Id, character.Id);
                await _mongoCollection.DeleteOneAsync(deleteFilter);
                break;
        }
    }
}