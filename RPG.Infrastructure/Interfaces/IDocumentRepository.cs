using RPG.Domain.Common;

namespace RPG.Infrastructure.Interfaces;

public interface IDocumentRepository
{
    Task UpsertAsync<TEntity>( TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class, IDomainEntity;
    Task<TEntity?> GetByIdAsync<TEntity>(object id, CancellationToken cancellationToken = default)
        where TEntity : class, IDomainEntity;
    Task<List<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default)
        where TEntity : class, IDomainEntity;
    Task<List<TEntity>> GetBatchAsync<TEntity>(int skip, int limit, CancellationToken cancellationToken = default)
        where TEntity : class, IDomainEntity;
    Task<long> CountAsync<TEntity>(CancellationToken cancellationToken = default)
        where TEntity : class, IDomainEntity;
    Task<bool> DeleteAsync<TEntity>(object id, CancellationToken cancellationToken = default)
        where TEntity : class, IDomainEntity;
}

