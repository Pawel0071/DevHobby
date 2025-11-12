using RPG.Infrastructure.Interfaces;

namespace RedisWarmUp.Services;

/// <summary>
///     Strategy for warming up a specific document type from MongoDB to Redis
/// </summary>
public interface IDocumentWarmUpStrategy
{
    /// <summary>
    ///     Gets the collection name for this document type
    /// </summary>
    string CollectionName { get; }

    /// <summary>
    ///     Loads all documents from the source repository and writes them to the destination repository (Redis).
    /// </summary>
    /// <param name="redisRepository">The destination Redis repository.</param>
    /// <param name="logger">The logger for logging progress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of documents processed.</returns>
    Task<int> WarmUpAsync(IRedisRepository redisRepository,
        RPG.Infrastructure.Interfaces.ILogger<RedisWarmUpService> logger, CancellationToken cancellationToken = default);
}