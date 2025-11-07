using System.Text.Json;

namespace RPG.Infrastructure.Interfaces;

/// <summary>
/// Repository for reading documents from MongoDB for cache warming
/// </summary>
public interface IMongoDocumentReader
{
    /// <summary>
    /// Reads all documents from a specific collection
    /// </summary>
    Task<List<Dictionary<string, JsonElement>>> ReadAllAsync(string collectionName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reads documents from a collection with pagination
    /// </summary>
    Task<List<Dictionary<string, JsonElement>>> ReadBatchAsync(string collectionName, int skip, int limit, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of documents in a collection
    /// </summary>
    Task<long> GetCountAsync(string collectionName, CancellationToken cancellationToken = default);
}
