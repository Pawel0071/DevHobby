using Newtonsoft.Json;
using RPG.Infrastructure.Interfaces;
using StackExchange.Redis;

namespace RPG.Infrastructure.Redis;

public class RedisCache : IRedisCache
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCache> _logger;

    public RedisCache(IConnectionMultiplexer redis, ILogger<RedisCache> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.HasValue)
            {
                _logger.Debug($"Cache hit for key: {key}");
                return JsonConvert.DeserializeObject<T>(value!);
            }

            _logger.Debug($"Cache miss for key: {key}");
            return default;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error retrieving key '{key}' from Redis.", ex);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        try
        {
            var json = JsonConvert.SerializeObject(value);
            await _db.StringSetAsync(key, json, ttl);
            _logger.Debug($"Cache set for key: {key} (TTL: {ttl?.TotalSeconds ?? 0}s)");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error setting key '{key}' in Redis.", ex);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
            _logger.Debug($"Cache removed for key: {key}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error removing key '{key}' from Redis.", ex);
        }
    }
}