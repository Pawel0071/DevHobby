namespace RPG.Infrastructure.Documents;

/// <summary>
///     Interface for MongoDB documents that defines collection name
/// </summary>
public interface IMongoDocument
{
    /// <summary>
    ///     MongoDB collection name for this document type
    /// </summary>
    static abstract string CollectionName { get; }
    
    /// <summary>
    ///     Document unique identifier
    /// </summary>
    Guid Id { get; set; }
}
