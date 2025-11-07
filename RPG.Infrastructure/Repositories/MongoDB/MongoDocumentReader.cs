using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.MongoDB;

public class MongoDocumentReader : IMongoDocumentReader
{
    private readonly IMongoDatabase _database;
    private readonly Interfaces.ILogger<MongoDocumentReader> _logger;

    public MongoDocumentReader(
        IMongoDatabase database,
        Interfaces.ILogger<MongoDocumentReader> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<List<Dictionary<string, JsonElement>>> ReadAllAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            var bsonDocuments = await collection.Find(_ => true).ToListAsync(cancellationToken);
            
            var documents = ConvertBsonToJsonDictionary(bsonDocuments);
            
            _logger.Info($"Read {documents.Count} documents from {collectionName}");
            
            return documents;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error reading documents from {collectionName}", ex);
            throw;
        }
    }

    public async Task<List<Dictionary<string, JsonElement>>> ReadBatchAsync(string collectionName, int skip, int limit, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            var bsonDocuments = await collection.Find(_ => true)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync(cancellationToken);
            
            var documents = ConvertBsonToJsonDictionary(bsonDocuments);
            
            _logger.Debug($"Read batch of {documents.Count} documents from {collectionName} (skip={skip}, limit={limit})");
            
            return documents;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error reading batch from {collectionName}", ex);
            throw;
        }
    }

    public async Task<long> GetCountAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            var count = await collection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);
            
            _logger.Debug($"Collection {collectionName} has {count} documents");
            
            return count;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error counting documents in {collectionName}", ex);
            throw;
        }
    }

    private List<Dictionary<string, JsonElement>> ConvertBsonToJsonDictionary(List<BsonDocument> bsonDocuments)
    {
        var result = new List<Dictionary<string, JsonElement>>();

        foreach (var bsonDoc in bsonDocuments)
        {
            // Convert BSON to JSON string, then parse to Dictionary<string, JsonElement>
            var jsonString = bsonDoc.ToJson(new global::MongoDB.Bson.IO.JsonWriterSettings 
            { 
                OutputMode = global::MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson 
            });
            var jsonDoc = JsonDocument.Parse(jsonString);
            
            var dict = new Dictionary<string, JsonElement>();
            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                // Convert "_id" to "Id" for consistency
                var key = property.Name == "_id" ? "Id" : property.Name;
                dict[key] = property.Value.Clone();
            }
            
            result.Add(dict);
        }

        return result;
    }
}
