using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using RPG.Domain.Common.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.Orchestrators;

/// <summary>
///     Repository for loading dictionary definitions (ErrorCodeDefinition, TagDefinition)
///     from MongoDB into memory at application startup.
/// </summary>
/// <typeparam name="T">Dictionary type that implements IDictionaryEntry</typeparam>
public class DictionaryRepository<T> : IDictionaryRepository<T> where T : IDictionaryEntry<T>
{
    private readonly IMongoCollection<T> _collection;
    private readonly ILogger<DictionaryRepository<T>> _logger;

    public DictionaryRepository(
        IMongoDatabase database,
        ILogger<DictionaryRepository<T>> logger)
    {
        var collectionName = GetCollectionName();
        _collection = database.GetCollection<T>(collectionName);
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // handle static dictionaries in-memory
        if (typeof(IStaticDictionaryDefinition).IsAssignableFrom(typeof(T)))
        {
            var predefined = T.Predefined.ToList();
            _logger.Debug($"Using in-memory predefined entries for {typeof(T).Name}: {predefined.Count}");
            return predefined.AsReadOnly();
        }

        try
        {
            _logger.Debug($"Loading all {typeof(T).Name} from MongoDB collection: {GetCollectionName()}");

            using var cursor = await _collection.FindAsync(_ => true, cancellationToken: cancellationToken);
            var items = await ReadAllAsync(cursor, cancellationToken).ConfigureAwait(false);

            _logger.Info($"Loaded {items.Count} {typeof(T).Name} entries from MongoDB");

            return items.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load {typeof(T).Name} from MongoDB", ex);
            throw;
        }
    }

    public async Task UpsertManyAsync(IEnumerable<T> entries, CancellationToken cancellationToken = default)
    {
        if (entries is null) return;

        // skip Mongo upserts for static dictionaries
        if (typeof(IStaticDictionaryDefinition).IsAssignableFrom(typeof(T)))
        {
            _logger.Debug($"Skipping Mongo upsert for static dictionary {typeof(T).Name}");
            return;
        }

        var models = entries
            .Where(entry => entry is not null)
            .Select(entry =>
            {
                var filter = Builders<T>.Filter.Eq(x => x.Code, entry.Code);
                return new ReplaceOneModel<T>(filter, entry) { IsUpsert = true };
            })
            .ToList();

        if (models.Count == 0)
        {
            return;
        }

        try
        {
            _logger.Debug($"Ensuring {models.Count} {typeof(T).Name} definitions exist in MongoDB collection: {GetCollectionName()}");
            await _collection.BulkWriteAsync(models, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to upsert {typeof(T).Name} definitions", ex);
            throw;
        }
    }

    public async Task<T?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Debug($"Loading {typeof(T).Name} with code: {code}");

            var filter = Builders<T>.Filter.Eq(x => x.Code, code);
            using var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
            var item = await ReadFirstOrDefaultAsync(cursor, cancellationToken).ConfigureAwait(false);

            if (item != null)
                _logger.Debug($"Found {typeof(T).Name} with code: {code}");
            else
                _logger.Warn($"{typeof(T).Name} with code '{code}' not found");

            return item;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load {typeof(T).Name} with code '{code}'", ex);
            throw;
        }
    }

    private static string GetCollectionName()
    {
        var typeName = typeof(T).Name;

    // ErrorCodeDefinition → ErrorCodes
    // TagDefinition → Tags
        if (typeName.EndsWith("Definition")) typeName = typeName[..^"Definition".Length];

        return typeName + "s";
    }

    private static async Task<List<T>> ReadAllAsync(IAsyncCursor<T> cursor, CancellationToken cancellationToken)
    {
        var items = new List<T>();

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            items.AddRange(cursor.Current.Where(entry => entry is not null));
        }

        return items;
    }

    private static async Task<T?> ReadFirstOrDefaultAsync(IAsyncCursor<T> cursor, CancellationToken cancellationToken)
    {
        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var entry in cursor.Current)
            {
                if (entry is not null)
                {
                    return entry;
                }
            }
        }

        return default;
    }
}
