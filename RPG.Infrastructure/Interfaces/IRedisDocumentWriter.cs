namespace RPG.Infrastructure.Interfaces;

/// <summary>
/// Repository for writing documents to Redis cache
/// </summary>
public interface IRedisDocumentWriter
{
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
