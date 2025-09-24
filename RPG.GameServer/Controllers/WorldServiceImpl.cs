using RabbitMQ.Client;
using RPG.GameServer.Protos;
using StackExchange.Redis;

namespace RPG.GameServer.Controlers;

public class  WorldServiceImpl : WorldService.WorldServiceBase
{
    private readonly IDatabase _redis;
    private readonly IModel _rabbitChannel;

    public WorldServiceImpl (IConnectionMultiplexer redis, IModel rabbitChannel)
    {
        _redis = redis.GetDatabase();
        _rabbitChannel = rabbitChannel;
    }
    
}