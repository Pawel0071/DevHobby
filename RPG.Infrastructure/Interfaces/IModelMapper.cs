using RPG.Domain.Common;
using RPG.Infrastructure.Models;

namespace RPG.Infrastructure.Interfaces;

/// <summary>
///     Konwersja miedzy modelem domenowym (<see cref="IDomainModel"/>) i modelem trwałości (<see cref="IPersistenceModel"/>).
///     Celem jest bezproblemowa serializacja do JSON (Redis/RabbitMQ) i BSON (MongoDB).
///     Uwaga: Implementacje mogą tworzyć opcjonalne komponenty na podstawie tagów.
/// </summary>
public interface IModelMapper<TDomainModel, TPersistenceModel>
    where TDomainModel : class, IDomainModel
    where TPersistenceModel : class, IPersistenceModel
{
    /// <summary>
    ///     Mapuje encję domenową na dokument do zapisu (bezpieczny do serializacji do JSON/BSON).
    /// </summary>
    TPersistenceModel ToPersistence(TDomainModel entity);

    /// <summary>
    ///     Mapuje dokument z bazy/transportu na encję domenową (może utworzyć brakujące komponenty wynikające z tagów).
    /// </summary>
    TDomainModel ToDomain(TPersistenceModel document);
}
