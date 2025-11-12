using RPG.Domain.Common;

namespace RPG.Infrastructure.Interfaces;

public interface IModelRepository
{
    Task UpsertAsync<TDomainModel>( TDomainModel entity, CancellationToken cancellationToken = default)
        where TDomainModel : class, IDomainModel;
    Task<TDomainModel?> GetByIdAsync<TDomainModel>(object id, CancellationToken cancellationToken = default)
        where TDomainModel : class, IDomainModel;
    Task<List<TDomainModel>> GetAllAsync<TDomainModel>(CancellationToken cancellationToken = default)
        where TDomainModel : class, IDomainModel;
    Task<List<TDomainModel>> GetBatchAsync<TDomainModel>(int skip, int limit, CancellationToken cancellationToken = default)
        where TDomainModel : class, IDomainModel;
    Task<long> CountAsync<TDomainModel>(CancellationToken cancellationToken = default)
        where TDomainModel : class, IDomainModel;
    Task<bool> DeleteAsync<TDomainModel>(object id, CancellationToken cancellationToken = default)
        where TDomainModel : class, IDomainModel;
}

