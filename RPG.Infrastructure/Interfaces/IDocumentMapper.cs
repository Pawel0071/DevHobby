namespace RPG.Infrastructure.Interfaces;

/// <summary>
/// Mapper for converting between domain entities and MongoDB documents
/// </summary>
/// <typeparam name="TEntity">Domain entity type</typeparam>
/// <typeparam name="TDocument">MongoDB document type</typeparam>
public interface IDocumentMapper<TEntity, TDocument> where TEntity : class where TDocument : class
{
    /// <summary>
    /// Converts a domain entity to a MongoDB document
    /// </summary>
    TDocument ToDocument(TEntity entity);
    
    /// <summary>
    /// Converts a MongoDB document to a domain entity
    /// </summary>
    TEntity ToDomain(TDocument document);
}
