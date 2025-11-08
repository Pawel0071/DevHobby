using RPG.Infrastructure.Documents;

namespace RPG.Infrastructure.Interfaces;

/// <summary>
///     Repository interface for Redis CRUD operations on typed documents.
///     Identical to IMongoDocumentRepository - methods are generic, class is not.
/// </summary>
public interface IRedisDocumentRepository
{
    /// <summary>
    ///     Insert or update a document
    /// </summary>
    Task UpsertAsync<TDocument>(TDocument document, CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument;
    
    /// <summary>
    ///     Get a document by its ID
    /// </summary>
    Task<TDocument?> GetByIdAsync<TDocument>(object id, CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument;
    
    /// <summary>
    ///     Get all documents
    /// </summary>
    Task<List<TDocument>> GetAllAsync<TDocument>(CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument;
    
    /// <summary>
    ///     Get documents in batches
    /// </summary>
    Task<List<TDocument>> GetBatchAsync<TDocument>(int skip, int limit, CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument;
    
    /// <summary>
    ///     Count total documents
    /// </summary>
    Task<long> CountAsync<TDocument>(CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument;
    
    /// <summary>
    ///     Delete a document by its ID
    /// </summary>
    Task<bool> DeleteAsync<TDocument>(object id, CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument;
}

