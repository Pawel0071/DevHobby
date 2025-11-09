using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.PersistenceService.Services;

/// <summary>
///     Generic strategy implementation for persisting documents
/// </summary>
public class DocumentPersistenceStrategy<TDocument> : IDocumentPersistenceStrategy where TDocument : class, IMongoDocument
{
    private readonly IMongoDocumentRepository _repository;
    public string CollectionName { get; }

    public DocumentPersistenceStrategy(IMongoDocumentRepository repository, string collectionName)
    {
        _repository = repository;
        CollectionName = collectionName;
    }

    public async Task UpsertAsync(IMongoDocument document, CancellationToken cancellationToken)
    {
        if (document is TDocument typedDocument)
        {
            await _repository.UpsertAsync(typedDocument, cancellationToken);
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync<TDocument>(id, cancellationToken);
    }
}
