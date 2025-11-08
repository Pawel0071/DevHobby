using RPG.Infrastructure.Documents;

namespace RPG.Infrastructure.Interfaces;

/// <summary>
///     Repository interface for MongoDB CRUD operations on typed documents.
///     Methods are generic - class is not.
/// </summary>
public interface IMongoDocumentRepository
{
    /// <summary>

    Task UpsertAsync<TDocument>(TDocument document, CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument;
    Task<TDocument?> GetByIdAsync<TDocument>(object id, CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument;
    Task<List<TDocument>> GetAllAsync<TDocument>(CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument;
        Task<List<TDocument>> GetBatchAsync<TDocument>(int skip, int limit, CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument;
    Task<long> CountAsync<TDocument>(CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument;
    Task<bool> DeleteAsync<TDocument>(object id, CancellationToken cancellationToken = default) 
        where TDocument : class, IMongoDocument;
}
