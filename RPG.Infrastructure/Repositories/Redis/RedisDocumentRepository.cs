using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using StackExchange.Redis;

namespace RPG.Infrastructure.Repositories.Redis;

/// <summary>
///     Redis document repository - identical interface to MongoDocumentRepository.
///     Uses CollectionName from IMongoDocument to build Redis keys.
/// </summary>
public class RedisDocumentRepository : IRedisDocumentRepository
{
    private readonly ILogger<RedisDocumentRepository> _logger;
    private readonly IDatabase _redisDatabase;
    private readonly IActivityScope _activityScope;

    public RedisDocumentRepository(
        IDatabase redisDatabase,
        ILogger<RedisDocumentRepository> logger,
        IActivityScope activityScope)
    {
        _redisDatabase = redisDatabase;
        _logger = logger;
        _activityScope = activityScope;
    }

    /// <summary>
    ///     Build Redis key for a document: CollectionName:Id
    /// </summary>
    private string BuildKey<TDocument>(object id) where TDocument : class, IMongoDocument
    {
        return $"{TDocument.CollectionName}:{id}";
    }

    /// <summary>
    ///     Build Redis pattern for a collection: CollectionName:*
    /// </summary>
    private string BuildPattern<TDocument>() where TDocument : class, IMongoDocument
    {
        return $"{TDocument.CollectionName}:*";
    }

    /// <summary>
    ///     Insert or update a document in Redis
    /// </summary>
    public async Task UpsertAsync<TDocument>(TDocument document, CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument
    {
        try
        {
            using var activity = _activityScope.Start("redis.upsert", new Dictionary<string, object>
            {
                ["db.system"] = "redis",
                ["db.operation"] = "set",
                ["db.redis.key"] = BuildKey<TDocument>(document.Id),
                ["db.redis.database"] = _redisDatabase.Database
            });

            var key = BuildKey<TDocument>(document.Id);
            var json = JsonSerializer.Serialize(document);
            
            var success = await _redisDatabase.StringSetAsync(key, json);

            if (success)
                _logger.Info($"{typeof(TDocument).Name} upserted to Redis. Id={document.Id}");
            else
                _logger.Warn($"Failed to upsert {typeof(TDocument).Name} to Redis. Id={document.Id}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to upsert {typeof(TDocument).Name} to Redis", ex);
            throw;
        }
    }

    /// <summary>
    ///     Get a document by its ID from Redis
    /// </summary>
    public async Task<TDocument?> GetByIdAsync<TDocument>(object id, CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument
    {
        try
        {
            using var activity = _activityScope.Start("redis.get", new Dictionary<string, object>
            {
                ["db.system"] = "redis",
                ["db.operation"] = "get",
                ["db.redis.key"] = BuildKey<TDocument>(id),
                ["db.redis.database"] = _redisDatabase.Database
            });

            var key = BuildKey<TDocument>(id);
            var json = await _redisDatabase.StringGetAsync(key);

            if (!json.HasValue)
            {
                _logger.Debug($"{typeof(TDocument).Name} not found in Redis. Id={id}");
                return null;
            }

            var document = JsonSerializer.Deserialize<TDocument>(json.ToString());
            _logger.Debug($"{typeof(TDocument).Name} found in Redis. Id={id}");

            return document;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get {typeof(TDocument).Name} from Redis. Id={id}", ex);
            throw;
        }
    }

    /// <summary>
    ///     Get all documents of a specific type from Redis
    /// </summary>
    public async Task<List<TDocument>> GetAllAsync<TDocument>(CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument
    {
        try
        {
            using var activity = _activityScope.Start("redis.getAll", new Dictionary<string, object>
            {
                ["db.system"] = "redis",
                ["db.operation"] = "scan",
                ["db.redis.pattern"] = BuildPattern<TDocument>(),
                ["db.redis.database"] = _redisDatabase.Database
            });

            var pattern = BuildPattern<TDocument>();
            var server = _redisDatabase.Multiplexer.GetServer(_redisDatabase.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).Select(k => (RedisKey)k.ToString()).ToArray();

            if (keys.Length == 0)
            {
                _logger.Debug($"No {typeof(TDocument).Name} documents found in Redis");
                return new List<TDocument>();
            }

            var values = await _redisDatabase.StringGetAsync(keys);
            var documents = new List<TDocument>();

            foreach (var value in values)
            {
                if (!value.HasValue) continue;
                
                try
                {
                    var document = JsonSerializer.Deserialize<TDocument>(value.ToString());
                    if (document != null) documents.Add(document);
                }
                catch (JsonException ex)
                {
                    _logger.Warn($"Failed to deserialize {typeof(TDocument).Name}: {ex.Message}");
                }
            }

            _logger.Info($"Read {documents.Count} {typeof(TDocument).Name} documents from Redis");
            return documents;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get all {typeof(TDocument).Name} from Redis", ex);
            throw;
        }
    }

    /// <summary>
    ///     Get documents in batches (pagination)
    /// </summary>
    public async Task<List<TDocument>> GetBatchAsync<TDocument>(int skip, int limit, CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument
    {
        try
        {
            using var activity = _activityScope.Start("redis.getBatch", new Dictionary<string, object>
            {
                ["db.system"] = "redis",
                ["db.operation"] = "scan",
                ["db.redis.pattern"] = BuildPattern<TDocument>(),
                ["db.redis.database"] = _redisDatabase.Database,
                ["db.redis.skip"] = skip,
                ["db.redis.limit"] = limit
            });

            var allDocuments = await GetAllAsync<TDocument>(cancellationToken);
            var batch = allDocuments.Skip(skip).Take(limit).ToList();

            _logger.Debug($"Read batch of {batch.Count} {typeof(TDocument).Name} documents from Redis (skip={skip}, limit={limit})");
            return batch;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get batch of {typeof(TDocument).Name} from Redis", ex);
            throw;
        }
    }

    /// <summary>
    ///     Count total documents of a specific type in Redis
    /// </summary>
    public Task<long> CountAsync<TDocument>(CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument
    {
        try
        {
            using var activity = _activityScope.Start("redis.count", new Dictionary<string, object>
            {
                ["db.system"] = "redis",
                ["db.operation"] = "scan",
                ["db.redis.pattern"] = BuildPattern<TDocument>(),
                ["db.redis.database"] = _redisDatabase.Database
            });

            var pattern = BuildPattern<TDocument>();
            var server = _redisDatabase.Multiplexer.GetServer(_redisDatabase.Multiplexer.GetEndPoints().First());
            var count = server.Keys(pattern: pattern).Count();

            _logger.Debug($"Collection {typeof(TDocument).Name} has {count} documents in Redis");
            return Task.FromResult((long)count);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to count {typeof(TDocument).Name} documents in Redis", ex);
            throw;
        }
    }

    /// <summary>
    ///     Delete a document by its ID from Redis
    /// </summary>
    public async Task<bool> DeleteAsync<TDocument>(object id, CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument
    {
        try
        {
            using var activity = _activityScope.Start("redis.delete", new Dictionary<string, object>
            {
                ["db.system"] = "redis",
                ["db.operation"] = "del",
                ["db.redis.key"] = BuildKey<TDocument>(id),
                ["db.redis.database"] = _redisDatabase.Database
            });

            var key = BuildKey<TDocument>(id);
            var deleted = await _redisDatabase.KeyDeleteAsync(key);

            if (deleted)
            {
                _logger.Info($"{typeof(TDocument).Name} deleted from Redis. Id={id}");
                return true;
            }

            _logger.Warn($"{typeof(TDocument).Name} not found in Redis for deletion. Id={id}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to delete {typeof(TDocument).Name} from Redis. Id={id}", ex);
            throw;
        }
    }
}

