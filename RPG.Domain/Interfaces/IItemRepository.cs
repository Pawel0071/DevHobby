using RPG.Domain.Common;

namespace RPG.Domain.Interfaces;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(Guid id);
    Task<Item?> GetByNameAsync(string name);
    Task SaveAsync(Item item);
    Task<Item?> TryGetFromCacheAsync(Guid id);             
    Task<Item?> TryGetFromDatabaseAsync(Guid id);    
}