using RPG.Domain.Common;
using RPG.Infrastructure.Documents;

namespace RPG.Infrastructure.Interfaces;

public interface IModelMapper<TDomainModel, TPersistenceModel>
    where TDomainModel : class, IDomainModel
    where TPersistenceModel : class, IPersistenceModel
{
    TPersistenceModel ToPersistence(TDomainModel entity);
    TDomainModel ToDomain(TPersistenceModel document);
}
