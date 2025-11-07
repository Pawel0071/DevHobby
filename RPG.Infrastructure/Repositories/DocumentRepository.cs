using System.Text.Json;
using MongoDB.Driver;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<OutboxMessage> _outboxCollection;
    private readonly Interfaces.ILogger<DocumentRepository> _logger;

    public DocumentRepository(
        IMongoDatabase database,
        Interfaces.ILogger<DocumentRepository> logger)
    {
        _database = database;
        _outboxCollection = database.GetCollection<OutboxMessage>("OutboxMessages");
        _logger = logger;
    }

    public async Task UpsertAsync(string collectionName, Dictionary<string, JsonElement> document, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _database.GetCollection<Dictionary<string, JsonElement>>(collectionName);

            if (document.TryGetValue("Id", out var idElement) || document.TryGetValue("id", out idElement))
            {
                var id = idElement.GetGuid();
                var filter = Builders<Dictionary<string, JsonElement>>.Filter.Or(
                    Builders<Dictionary<string, JsonElement>>.Filter.Eq("Id", id),
                    Builders<Dictionary<string, JsonElement>>.Filter.Eq("id", id)
                );

                var options = new ReplaceOptions { IsUpsert = true };
                var result = await collection.ReplaceOneAsync(filter, document, options, cancellationToken);

                _logger.Info($"Document upserted in {collectionName}. Id={id}, Matched={result.MatchedCount}, Modified={result.ModifiedCount}");
            }
            else
            {
                await collection.InsertOneAsync(document, cancellationToken: cancellationToken);
                _logger.Info($"Document inserted in {collectionName} (no ID found)");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error upserting document in {collectionName}", ex);
            throw;
        }
    }

    public async Task DeleteAsync(string collectionName, Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _database.GetCollection<Dictionary<string, JsonElement>>(collectionName);
            var filter = Builders<Dictionary<string, JsonElement>>.Filter.Or(
                Builders<Dictionary<string, JsonElement>>.Filter.Eq("Id", id),
                Builders<Dictionary<string, JsonElement>>.Filter.Eq("id", id)
            );

            var result = await collection.DeleteOneAsync(filter, cancellationToken);

            _logger.Info($"Document deleted from {collectionName}. Id={id}, DeletedCount={result.DeletedCount}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting document from {collectionName}. Id={id}", ex);
            throw;
        }
    }

    public async Task SaveToOutboxAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var outboxMessage = new OutboxMessage
            {
                Topic = topic,
                Payload = payload,
                Sent = true,
                CreatedAt = DateTime.UtcNow
            };

            await _outboxCollection.InsertOneAsync(outboxMessage, cancellationToken: cancellationToken);

            _logger.Debug($"Message saved to Outbox. Topic={topic}, Id={outboxMessage.Id}");
        }
        catch
        {
            _logger.Warn($"Failed to save message to Outbox (non-critical). Topic={topic}");
            // Nie rzucamy wyjątku - to tylko audit log
        }
    }
}
