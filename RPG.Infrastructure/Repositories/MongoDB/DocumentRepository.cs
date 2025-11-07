using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.MongoDB;

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
            // Convert Dictionary<string, JsonElement> to BsonDocument
            var bsonDocument = ConvertToBsonDocument(document);
            var collection = _database.GetCollection<BsonDocument>(collectionName);

            if (document.TryGetValue("Id", out var idElement) || document.TryGetValue("id", out idElement))
            {
                // Handle different ID formats (GUID string, ObjectId, native GUID)
                Guid id;
                if (idElement.ValueKind == JsonValueKind.Object && idElement.TryGetProperty("$oid", out var oidProperty))
                {
                    // MongoDB ObjectId format { "$oid": "..." }
                    var oidString = oidProperty.GetString();
                    id = Guid.Parse(oidString ?? throw new InvalidOperationException("ObjectId is null"));
                }
                else if (idElement.ValueKind == JsonValueKind.String)
                {
                    // GUID as string
                    id = Guid.Parse(idElement.GetString() ?? throw new InvalidOperationException("Id is null"));
                }
                else
                {
                    // Native GUID in JSON
                    id = idElement.GetGuid();
                }
                
                var filter = Builders<BsonDocument>.Filter.Or(
                    Builders<BsonDocument>.Filter.Eq("Id", id.ToString()),
                    Builders<BsonDocument>.Filter.Eq("id", id.ToString())
                );

                var options = new ReplaceOptions { IsUpsert = true };
                var result = await collection.ReplaceOneAsync(filter, bsonDocument, options, cancellationToken);

                _logger.Info($"Document upserted in {collectionName}. Id={id}, Matched={result.MatchedCount}, Modified={result.ModifiedCount}, UpsertedId={result.UpsertedId}");
            }
            else
            {
                var bsonDoc = ConvertToBsonDocument(document);
                await collection.InsertOneAsync(bsonDoc, cancellationToken: cancellationToken);
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
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            var filter = Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Eq("Id", id.ToString()),
                Builders<BsonDocument>.Filter.Eq("id", id.ToString())
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
    
    private static BsonDocument ConvertToBsonDocument(Dictionary<string, JsonElement> dictionary)
    {
        var bsonDocument = new BsonDocument();
        
        foreach (var kvp in dictionary)
        {
            bsonDocument[kvp.Key] = ConvertJsonElementToBsonValue(kvp.Value);
        }
        
        return bsonDocument;
    }
    
    private static BsonValue ConvertJsonElementToBsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => BsonValue.Create(element.GetString()),
            JsonValueKind.Number => element.TryGetInt32(out var intValue)
                ? BsonValue.Create(intValue)
                : element.TryGetInt64(out var longValue)
                    ? BsonValue.Create(longValue)
                    : BsonValue.Create(element.GetDouble()),
            JsonValueKind.True => BsonValue.Create(true),
            JsonValueKind.False => BsonValue.Create(false),
            JsonValueKind.Null => BsonNull.Value,
            JsonValueKind.Object => ConvertJsonObjectToBsonDocument(element),
            JsonValueKind.Array => ConvertJsonArrayToBsonArray(element),
            _ => BsonNull.Value
        };
    }
    
    private static BsonDocument ConvertJsonObjectToBsonDocument(JsonElement element)
    {
        var bsonDoc = new BsonDocument();
        foreach (var property in element.EnumerateObject())
        {
            bsonDoc[property.Name] = ConvertJsonElementToBsonValue(property.Value);
        }
        return bsonDoc;
    }
    
    private static BsonArray ConvertJsonArrayToBsonArray(JsonElement element)
    {
        var bsonArray = new BsonArray();
        foreach (var item in element.EnumerateArray())
        {
            bsonArray.Add(ConvertJsonElementToBsonValue(item));
        }
        return bsonArray;
    }
}
