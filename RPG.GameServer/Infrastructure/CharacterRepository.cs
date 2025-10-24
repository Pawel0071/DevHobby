using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RPG.Core.Domain.Entities;
using RPG.GameServer.Interfaces;
using StackExchange.Redis;

namespace RPG.GameServer.Infrastructure;

public class CharacterRepository : ICharacterRepository
{
    private readonly IDatabase _redis;
    private readonly IModel _rabbitChannel;

    private const string RedisPrefix = "character:";
    private const string ExchangeName = "rpg_exchange";

    public CharacterRepository(IConnectionMultiplexer redisConnection, IConnection rabbitConnection)
    {
        _redis = redisConnection.GetDatabase();
        _rabbitChannel = rabbitConnection.CreateModel();
        _rabbitChannel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
    }

    public async Task<Character> CreateAsync(Character character)
    {
        var json = JsonSerializer.Serialize(character);
        await _redis.StringSetAsync($"{RedisPrefix}{character.Id}", json);
        PublishEvent("character.created", json, character.Id);
        return character;
    }

    public async Task<Character?> GetAsync(string id)
    {
        var json = await _redis.StringGetAsync($"{RedisPrefix}{id}");
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<Character>(json!);
    }

    public async Task<Character> UpdateAsync(Character character)
    {
        var json = JsonSerializer.Serialize(character);
        await _redis.StringSetAsync($"{RedisPrefix}{character.Id}", json);
        PublishEvent("character.updated", json, character.Id);
        return character;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var success = await _redis.KeyDeleteAsync($"{RedisPrefix}{id}");
        if (success)
        {
            var payload = JsonSerializer.Serialize(new { id });
            PublishEvent("character.deleted", payload, id);
        }
        return success;
    }

    private void PublishEvent(string routingKey, string payload, Guid characterId)
    {
        // Use the character ID as the Redis key
        var redisKey = $"event:{routingKey}:{characterId}";

        // Update the payload in Redis
        _redis.StringSet(redisKey, payload);

        // Publish the event to RabbitMQ
        var body = Encoding.UTF8.GetBytes(payload);
        _rabbitChannel.BasicPublish(
            exchange: ExchangeName,
            routingKey: routingKey,
            basicProperties: null,
            body: body
        );
    }
}