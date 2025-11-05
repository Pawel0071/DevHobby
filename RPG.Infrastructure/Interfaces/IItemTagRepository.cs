using RPG.Domain.Entities.Items;

namespace RPG.Infrastructure.Interfaces;

public interface IDictionaryRepository<T>
{
    Task<IReadOnlyCollection<T>> GetAllAsync(CancellationToken cancellationToken = default);
}