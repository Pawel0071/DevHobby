using RPG.Infrastructure.Documents;

namespace RPG.Infrastructure.Interfaces;

public interface IRepositoryBase
{
    Task UpsertAsync<TDocument>(TDocument document, CancellationToken cancellationToken = default)
        where TDocument : class, IPersistenceModel;
    Task<TDocument?> GetByIdAsync<TDocument>(object id, CancellationToken cancellationToken = default)
        where TDocument : class, IPersistenceModel;
    Task<List<TDocument>> GetAllAsync<TDocument>(CancellationToken cancellationToken = default)
        where TDocument : class, IPersistenceModel;
    Task<List<TDocument>> GetBatchAsync<TDocument>(int skip, int limit, CancellationToken cancellationToken = default)
        where TDocument : class, IPersistenceModel;
    Task<long> CountAsync<TDocument>(CancellationToken cancellationToken = default)
        where TDocument : class, IPersistenceModel;
    Task<bool> DeleteAsync<TDocument>(object id, CancellationToken cancellationToken = default)
        where TDocument : class, IPersistenceModel;
}
