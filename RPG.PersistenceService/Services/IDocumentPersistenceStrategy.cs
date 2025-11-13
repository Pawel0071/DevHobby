using RPG.Infrastructure.Models;

namespace RPG.PersistenceService.Services;

/// <summary>
///     Strategy interface for persisting documents to MongoDB collections
/// </summary>
public interface IDocumentPersistenceStrategy
{
    string CollectionName { get; }
    Task UpsertAsync(IPersistenceModel document, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
