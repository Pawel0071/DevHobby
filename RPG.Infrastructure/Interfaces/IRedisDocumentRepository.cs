namespace RPG.Infrastructure.Interfaces;

/// <summary>
/// Generic repository for reading and writing JSON documents to Redis cache.
/// Works with raw strings/JSON - does not depend on Domain entities.
/// </summary>
public interface IRedisDocumentRepository
{
    /// <summary>
    /// Reads a document from Redis by key
    /// </summary>
    Task<string?> ReadAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reads multiple documents from Redis by keys
    /// </summary>
    Task<Dictionary<string, string>> ReadBatchAsync(string[] keys, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Writes a document to Redis with a key pattern
    /// </summary>
    Task WriteAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Writes multiple documents to Redis in a batch
    /// </summary>
    Task WriteBatchAsync(Dictionary<string, string> keyValuePairs, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a key exists in Redis
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a key from Redis
    /// </summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
