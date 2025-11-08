using RPG.Domain.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RPG.Infrastructure.Interfaces
{
    public interface IDocumentRepositoryHandler<TEntity> where TEntity : class, IDomainEntity
    {
        Task UpsertAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
        Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<TEntity>> GetBatchAsync(int skip, int limit, CancellationToken cancellationToken = default);
        Task<long> CountAsync(CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default);
    }
}
