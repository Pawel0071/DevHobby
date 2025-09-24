using RabbitMQ.Client;
using RPG.GameServer.Protos;
using StackExchange.Redis;

namespace RPG.GameServer.Controlers;

public class InteractionServiceImpl : InteractionService.InteractionServiceBase
{
    private readonly IDatabase _redis;
    private readonly IModel _rabbitChannel;

    public InteractionServiceImpl (IConnectionMultiplexer redis, IModel rabbitChannel)
    {
        _redis = redis.GetDatabase();
        _rabbitChannel = rabbitChannel;
    }
    
}