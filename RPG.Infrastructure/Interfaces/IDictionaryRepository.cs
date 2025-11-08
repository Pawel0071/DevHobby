using RPG.Domain.Common.Interfaces;

namespace RPG.Infrastructure.Interfaces;

/// <summary>
///     Repository for dictionary definitions that are loaded from MongoDB at startup.
///     Used for: ErrorCodeDefinition, TagDefinition, etc.
/// </summary>
/// <typeparam name="T">Dictionary type that implements IDictionaryEntry</typeparam>
public interface IDictionaryRepository<T> where T : IDictionaryEntry<T>
{
    /// <summary>
    ///     Load all dictionary entries from MongoDB
    /// </summary>
    Task<IReadOnlyCollection<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upsert a collection of dictionary entries, ensuring predefined values exist.
    /// </summary>
    Task UpsertManyAsync(IEnumerable<T> entries, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Find a specific dictionary entry by its code
    /// </summary>
    Task<T?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
