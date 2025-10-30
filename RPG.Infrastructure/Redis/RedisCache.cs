using Newtonsoft.Json;
using RPG.Infrastructure.Interfaces;
using StackExchange.Redis;

namespace RPG.Infrastructure.Redis;

public class RedisCache : IRedisCache
{
    private readonly IDatabase _db;

    public RedisCache(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue
            ? JsonConvert.DeserializeObject<T>(value!)
            : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        var json = JsonConvert.SerializeObject(value);
        await _db.StringSetAsync(key, json, ttl);
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }
}