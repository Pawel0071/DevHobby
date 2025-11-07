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
            var collection = _database.GetCollection<Dictionary<string, JsonElement>>(collectionName);
            var documents = await collection.Find(_ => true).ToListAsync(cancellationToken);
            
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
            var collection = _database.GetCollection<Dictionary<string, JsonElement>>(collectionName);
            var documents = await collection.Find(_ => true)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync(cancellationToken);
            
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
            var collection = _database.GetCollection<Dictionary<string, JsonElement>>(collectionName);
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
}
