using System.Text.Json;

namespace RPG.Infrastructure.Interfaces;

/// <summary>
/// Repository for saving generic documents to MongoDB
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Saves or updates a document in a specific collection
    /// </summary>
    Task UpsertAsync(string collectionName, Dictionary<string, JsonElement> document, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a document from a specific collection by ID
    /// </summary>
    Task DeleteAsync(string collectionName, Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves a document to the outbox for audit purposes
    /// </summary>
    Task SaveToOutboxAsync(string topic, string payload, CancellationToken cancellationToken = default);
}
