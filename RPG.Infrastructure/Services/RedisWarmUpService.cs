using RPG.Infrastructure.Configuration;
using System.Text.Json;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Services;

public class RedisWarmUpService : IRedisWarmUpService
{
    private readonly IMongoDocumentReader _mongoReader;
    private readonly IRedisDocumentRepository _redisWriter;
    private readonly Interfaces.ILogger<RedisWarmUpService> _logger;
    private readonly RedisWarmUpSettings _settings;

    public RedisWarmUpService(
        IMongoDocumentReader mongoReader,
        IRedisDocumentRepository redisWriter,
        Interfaces.ILogger<RedisWarmUpService> logger,
        RedisWarmUpSettings settings)
    {
        _mongoReader = mongoReader;
        _redisWriter = redisWriter;
        _logger = logger;
        _settings = settings;
    }

    public async Task StartWarmUpAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Starting Redis warm-up service");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await WarmUpCycleAsync(cancellationToken);
                
                _logger.Info($"Warm-up cycle completed. Waiting {_settings.IntervalSeconds}s for next cycle");
                await Task.Delay(TimeSpan.FromSeconds(_settings.IntervalSeconds), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Info("Warm-up service stopped");
                break;
            }
            catch (Exception ex)
            {
                _logger.Error("Error in warm-up cycle", ex);
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }

    public async Task WarmUpCycleAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Starting warm-up cycle");

        var totalDocuments = 0;
        var totalWritten = 0;

        foreach (var collectionName in _settings.CollectionsToCache)
        {
            try
            {
                _logger.Info($"Processing collection: {collectionName}");

                var count = await _mongoReader.GetCountAsync(collectionName, cancellationToken);
                _logger.Info($"Found {count} documents in {collectionName}");

                if (count == 0)
                {
                    _logger.Warn($"Collection {collectionName} is empty, skipping");
                    continue;
                }

                // Process in batches
                var batchSize = _settings.BatchSize;
                var totalBatches = (int)Math.Ceiling((double)count / batchSize);

                for (int i = 0; i < totalBatches; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var skip = i * batchSize;
                    var documents = await _mongoReader.ReadBatchAsync(collectionName, skip, batchSize, cancellationToken);
                    
                    totalDocuments += documents.Count;

                    // Convert documents to Redis key-value pairs
                    var keyValuePairs = new Dictionary<string, string>();

                    foreach (var document in documents)
                    {
                        if (document.TryGetValue("Id", out var idElement) || document.TryGetValue("id", out idElement) || document.TryGetValue("_id", out idElement))
                        {
                            string idString;
                            
                            // Handle MongoDB ObjectId format: { "$oid": "..." }
                            if (idElement.ValueKind == JsonValueKind.Object && idElement.TryGetProperty("$oid", out var oidElement))
                            {
                                idString = oidElement.GetString()!;
                            }
                            else if (idElement.ValueKind == JsonValueKind.String)
                            {
                                idString = idElement.GetString()!;
                            }
                            else
                            {
                                _logger.Warn($"Document in {collectionName} has unsupported Id format: {idElement.ValueKind}, skipping");
                                continue;
                            }
                            
                            var key = $"{collectionName}:{idString}";
                            var value = JsonSerializer.Serialize(document);

                            keyValuePairs[key] = value;
                        }
                        else
                        {
                            _logger.Warn($"Document in {collectionName} has no Id field, skipping");
                        }
                    }

                    // Write batch to Redis
                    if (keyValuePairs.Count > 0)
                    {
                        var expiry = _settings.CacheExpirySeconds > 0 
                            ? TimeSpan.FromSeconds(_settings.CacheExpirySeconds) 
                            : (TimeSpan?)null;

                        await _redisWriter.WriteBatchAsync(keyValuePairs, expiry, cancellationToken);
                        totalWritten += keyValuePairs.Count;

                        _logger.Info($"Batch {i + 1}/{totalBatches} written to Redis ({keyValuePairs.Count} items)");
                    }
                }

                _logger.Info($"Completed {collectionName}: {totalWritten} documents cached");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing collection {collectionName}", ex);
            }
        }

        _logger.Info($"Warm-up cycle completed: {totalDocuments} documents read, {totalWritten} written to Redis");
    }
}
