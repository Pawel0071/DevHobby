using MongoDB.Driver;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.MongoDB;

/// <summary>
/// Generic MongoDB consumer that saves domain entities as documents
/// </summary>
/// <typeparam name="TEntity">Domain entity type</typeparam>
/// <typeparam name="TDocument">MongoDB document type</typeparam>
public class GenericMongoConsumer<TEntity, TDocument> : IMangoConsumer<TEntity> 
    where TEntity : class 
    where TDocument : class
{
    private readonly IMongoCollection<TDocument> _collection;
    private readonly IDocumentMapper<TEntity, TDocument> _mapper;
    private readonly ILogger<GenericMongoConsumer<TEntity, TDocument>> _logger;
    private readonly Func<TDocument, object> _idSelector;

    public GenericMongoConsumer(
        IMongoCollection<TDocument> collection,
        IDocumentMapper<TEntity, TDocument> mapper,
        ILogger<GenericMongoConsumer<TEntity, TDocument>> logger,
        Func<TDocument, object> idSelector)
    {
        _collection = collection;
        _mapper = mapper;
        _logger = logger;
        _idSelector = idSelector;
    }

    public async Task Consume(TEntity entity)
    {
        try
        {
            var entityType = typeof(TEntity).Name;
            _logger.Debug($"Saving {entityType} to MongoDB.");
            
            var document = _mapper.ToDocument(entity);
            var id = _idSelector(document);
            
            var filter = Builders<TDocument>.Filter.Eq("_id", id);
            await _collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true });
            
            _logger.Info($"{entityType} saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save {typeof(TEntity).Name} to MongoDB.", ex);
            throw;
        }
    }
}
