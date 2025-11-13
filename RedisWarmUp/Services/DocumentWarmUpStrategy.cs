using Microsoft.Extensions.Logging;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Models;

namespace RedisWarmUp.Services;

/// <summary>
///     Generic strategy implementation for warming up documents of type TDocument
/// </summary>
public class DocumentWarmUpStrategy<TDocument> : IDocumentWarmUpStrategy
    where TDocument : class, IPersistenceModel
{
    private readonly IMongoRepository _mongoRepository;
    public string CollectionName { get; }

    public DocumentWarmUpStrategy(IMongoRepository mongoRepository, string collectionName)
    {
        _mongoRepository = mongoRepository;
        CollectionName = collectionName;
    }

    public async Task<int> WarmUpAsync(IRedisRepository redisRepository,
        RPG.Infrastructure.Interfaces.ILogger<RedisWarmUpService> logger, CancellationToken cancellationToken = default)
    {
        logger.Info($"Loading collection: {CollectionName}");

        var documents = await _mongoRepository.GetAllAsync<TDocument>(cancellationToken);
        var documentList = documents.ToList();

        if (!documentList.Any())
        {
            logger.Info($"  ℹ️  {CollectionName}: empty, skipping");
            return 0;
        }

        logger.Info($"  📊 {CollectionName}: {documentList.Count} documents to load");

        var writtenCount = 0;
        foreach (var document in documentList)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await redisRepository.UpsertAsync(document, cancellationToken);
            writtenCount++;
        }

        logger.Info($"  ✅ {CollectionName}: {writtenCount} documents loaded to Redis");
        return writtenCount;
    }
}
