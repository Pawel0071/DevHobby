using System.Collections.Concurrent;
using Grpc.Core;
using RabbitMQ.Client;
using RPG.GameServer;
using RPG.GameServer.Protos;
using StackExchange.Redis;

namespace RPG.GameServer.Services;

public class CharacterServiceImpl : CharacterService.CharacterServiceBase
{
    private readonly IDatabase _redis;
    private readonly IModel _rabbitChannel;

    public CharacterServiceImpl (IConnectionMultiplexer redis, IModel rabbitChannel)
    {
        _redis = redis.GetDatabase();
        _rabbitChannel = rabbitChannel;
    }
    
}