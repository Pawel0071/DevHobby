namespace RPG.Infrastructure.Interfaces;

/// <summary>
///     Strategy for warming up a specific document type from MongoDB to Redis
/// </summary>
public interface IDocumentWarmUpStrategy
{
    /// <summary>
    ///     Gets the collection name for this document type
    /// </summary>
    string CollectionName { get; }
    
    /// <summary>
    ///     Loads all documents from MongoDB
    /// </summary>
    Task<IEnumerable<object>> GetAllDocumentsAsync(CancellationToken cancellationToken = default);
}
