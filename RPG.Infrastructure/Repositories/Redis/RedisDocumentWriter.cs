using RPG.Infrastructure.Interfaces;
using StackExchange.Redis;

namespace RPG.Infrastructure.Repositories.Redis;

public class RedisDocumentWriter : IRedisDocumentWriter
{
    private readonly IDatabase _redisDatabase;
    private readonly Interfaces.ILogger<RedisDocumentWriter> _logger;

    public RedisDocumentWriter(
        IDatabase redisDatabase,
        Interfaces.ILogger<RedisDocumentWriter> logger)
    {
        _redisDatabase = redisDatabase;
        _logger = logger;
    }

    public async Task WriteAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _redisDatabase.StringSetAsync(key, value, expiry);
            
            if (success)
            {
                _logger.Debug($"Written to Redis: {key} (expiry={expiry?.TotalSeconds ?? -1}s)");
            }
            else
            {
                _logger.Warn($"Failed to write to Redis: {key}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error writing to Redis: {key}", ex);
            throw;
        }
    }

    public async Task WriteBatchAsync(Dictionary<string, string> keyValuePairs, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var batch = _redisDatabase.CreateBatch();
            var tasks = new List<Task>();

            foreach (var kvp in keyValuePairs)
            {
                tasks.Add(batch.StringSetAsync(kvp.Key, kvp.Value, expiry));
            }

            batch.Execute();
            await Task.WhenAll(tasks);
            
            _logger.Info($"Written batch of {keyValuePairs.Count} items to Redis");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error writing batch to Redis", ex);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _redisDatabase.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error checking key existence in Redis: {key}", ex);
            throw;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _redisDatabase.KeyDeleteAsync(key);
            
            if (deleted)
            {
                _logger.Debug($"Deleted from Redis: {key}");
            }
            else
            {
                _logger.Debug($"Key not found in Redis: {key}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting from Redis: {key}", ex);
            throw;
        }
    }
}
