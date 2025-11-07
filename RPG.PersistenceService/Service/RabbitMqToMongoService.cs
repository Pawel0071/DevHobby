using System.Text;
using System.Text.Json;
using MongoDB.Driver;
using RabbitMQ.Client;
using RPG.Domain.Entities;

namespace RPG.PersistenceService.Service;

public class RabbitMqToMongoService : IRabbitMqToMongoService
{
    private IMongoCollection<Character> _mongoCollection;
    private readonly IChannel _rabbitChannel;
    private const string ExchangeName = "rpg_exchange";
    private const string QueueName = "rpg_queue";
    
    public RabbitMqToMongoService(IMongoCollection<Character> mongoCollection, IChannel rabbitChannel)
    {
        _mongoCollection = mongoCollection;
        _rabbitChannel = rabbitChannel;
    }


    

    public void StartListening()
    {
    }

}