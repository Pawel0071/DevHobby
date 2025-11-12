using System;
using System.Collections.Generic;
using MongoDB.Driver;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.MongoDB;

/// <summary>
///     MongoDB document repository for CRUD operations.
///     INTERNAL IMPLEMENTATION: Uses MongoDB, but consumers don't know this.
///     Methods are generic, not the class itself.
/// </summary>
public class MongoRepository : IMongoRepository
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoRepository> _logger;
    private readonly IActivityScope _activityScope;

    public MongoRepository(
        IMongoDatabase database,
        ILogger<MongoRepository> logger,
        IActivityScope activityScope)
    {
        _database = database;
        _logger = logger;
        _activityScope = activityScope;
    }

    /// <summary>
    ///     Insert or update a document in MongoDB
    /// </summary>
    public async Task UpsertAsync<TDocument>(TDocument document, CancellationToken cancellationToken = default)
        where TDocument : class, IPersistenceModel
    {
        try
        {
            using var activity = _activityScope.Start("mongo.upsert", new Dictionary<string, object>
            {
                ["db.system"] = "mongodb",
                ["db.operation"] = "replaceOne",
                ["db.collection"] = TDocument.CollectionName,
                ["db.namespace"] = GetDatabaseName()
            });

            var collection = _database.GetCollection<TDocument>(TDocument.CollectionName);
            var id = document.Id;
            var filter = Builders<TDocument>.Filter.Eq(d => d.Id, id);

            await collection.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);

            _logger.Info($"{typeof(TDocument).Name} upserted successfully. Id={id}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to upsert {typeof(TDocument).Name} to MongoDB", ex);
            throw;
        }
    }

    /// <summary>
    ///     Get a document by its ID
    /// </summary>
    public async Task<TDocument?> GetByIdAsync<TDocument>(object id, CancellationToken cancellationToken = default)
        where TDocument : class, IPersistenceModel
    {
        try
        {
            using var activity = _activityScope.Start("mongo.getById", new Dictionary<string, object>
            {
                ["db.system"] = "mongodb",
                ["db.operation"] = "findOne",
                ["db.collection"] = TDocument.CollectionName,
                ["db.namespace"] = GetDatabaseName(),
                ["db.mongo.request_id"] = id
            });

            var collection = _database.GetCollection<TDocument>(TDocument.CollectionName);
            var normalizedId = NormalizeId(id);
            var filter = Builders<TDocument>.Filter.Eq(d => d.Id, normalizedId);
            var document = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            if (document != null)
                _logger.Debug($"{typeof(TDocument).Name} found. Id={normalizedId}");
            else
                _logger.Debug($"{typeof(TDocument).Name} not found. Id={normalizedId}");

            return document;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get {typeof(TDocument).Name} from MongoDB. Id={id}", ex);
            throw;
        }
    }

    /// <summary>
    ///     Get all documents from the collection
    /// </summary>
    public async Task<List<TDocument>> GetAllAsync<TDocument>(CancellationToken cancellationToken = default)
        where TDocument : class, IPersistenceModel
    {
        try
        {
            using var activity = _activityScope.Start("mongo.findAll", new Dictionary<string, object>
            {
                ["db.system"] = "mongodb",
                ["db.operation"] = "find",
                ["db.collection"] = TDocument.CollectionName,
                ["db.namespace"] = GetDatabaseName()
            });

            var collection = _database.GetCollection<TDocument>(TDocument.CollectionName);
            var documents = await collection.Find(_ => true).ToListAsync(cancellationToken);

            _logger.Info($"Read {documents.Count} {typeof(TDocument).Name} documents from MongoDB");

            return documents;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get all {typeof(TDocument).Name} from MongoDB", ex);
            throw;
        }
    }

    /// <summary>
    ///     Get documents in batches (for large collections)
    /// </summary>
    public async Task<List<TDocument>> GetBatchAsync<TDocument>(int skip, int limit, CancellationToken cancellationToken = default)
        where TDocument : class, IPersistenceModel
    {
        try
        {
            using var activity = _activityScope.Start("mongo.findBatch", new Dictionary<string, object>
            {
                ["db.system"] = "mongodb",
                ["db.operation"] = "find",
                ["db.collection"] = TDocument.CollectionName,
                ["db.namespace"] = GetDatabaseName(),
                ["db.mongo.skip"] = skip,
                ["db.mongo.limit"] = limit
            });

            var collection = _database.GetCollection<TDocument>(TDocument.CollectionName);
            var documents = await collection
                .Find(_ => true)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync(cancellationToken);

            _logger.Debug(
                $"Read batch of {documents.Count} {typeof(TDocument).Name} documents (skip={skip}, limit={limit})");

            return documents;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get batch of {typeof(TDocument).Name} from MongoDB", ex);
            throw;
        }
    }

    /// <summary>
    ///     Count total documents in the collection
    /// </summary>
    public async Task<long> CountAsync<TDocument>(CancellationToken cancellationToken = default)
        where TDocument : class, IPersistenceModel
    {
        try
        {
            using var activity = _activityScope.Start("mongo.count", new Dictionary<string, object>
            {
                ["db.system"] = "mongodb",
                ["db.operation"] = "count",
                ["db.collection"] = TDocument.CollectionName,
                ["db.namespace"] = GetDatabaseName()
            });

            var collection = _database.GetCollection<TDocument>(TDocument.CollectionName);
            var count = await collection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);

            _logger.Debug($"Collection {typeof(TDocument).Name} has {count} documents");

            return count;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to count {typeof(TDocument).Name} documents", ex);
            throw;
        }
    }

    /// <summary>
    ///     Delete a document by its ID
    /// </summary>
    public async Task<bool> DeleteAsync<TDocument>(object id, CancellationToken cancellationToken = default)
        where TDocument : class, IPersistenceModel
    {
        try
        {
            using var activity = _activityScope.Start("mongo.delete", new Dictionary<string, object>
            {
                ["db.system"] = "mongodb",
                ["db.operation"] = "deleteOne",
                ["db.collection"] = TDocument.CollectionName,
                ["db.namespace"] = GetDatabaseName(),
                ["db.mongo.request_id"] = id
            });

            var collection = _database.GetCollection<TDocument>(TDocument.CollectionName);
            var normalizedId = NormalizeId(id);
            var filter = Builders<TDocument>.Filter.Eq(d => d.Id, normalizedId);
            var result = await collection.DeleteOneAsync(filter, cancellationToken);

            if (result.DeletedCount > 0)
            {
                _logger.Info($"{typeof(TDocument).Name} deleted. Id={normalizedId}");
                return true;
            }

            _logger.Warn($"{typeof(TDocument).Name} not found for deletion. Id={normalizedId}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to delete {typeof(TDocument).Name} from MongoDB. Id={id}", ex);
            throw;
        }

    }

    private string GetDatabaseName()
    {
        return _database?.DatabaseNamespace?.DatabaseName ?? "unknown";
    }

    private static Guid NormalizeId(object id)
    {
        return id switch
        {
            Guid guid => guid,
            string text when Guid.TryParse(text, out var parsed) => parsed,
            _ => throw new ArgumentException($"Unsupported identifier type: {id.GetType()}", nameof(id))
        };
    }
}
