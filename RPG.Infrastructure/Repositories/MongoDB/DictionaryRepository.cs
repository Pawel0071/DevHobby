using MongoDB.Driver;
using RPG.Domain.Common.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.Orchestrators;

/// <summary>
///     Repository for loading dictionary definitions (ErrorCodeDefinition, ItemTagDefinition, ItemTypeDefinition)
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
        try
        {
            _logger.Debug($"Loading all {typeof(T).Name} from MongoDB collection: {GetCollectionName()}");

            var items = await _collection
                .Find(_ => true)
                .ToListAsync(cancellationToken);

            _logger.Info($"Loaded {items.Count} {typeof(T).Name} entries from MongoDB");

            return items.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load {typeof(T).Name} from MongoDB", ex);
            throw;
        }
    }

    public async Task<T?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Debug($"Loading {typeof(T).Name} with code: {code}");

            var filter = Builders<T>.Filter.Eq(x => x.Code, code);
            var item = await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken);

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
        // ItemTagDefinition → ItemTags
        // ItemTypeDefinition → ItemTypes
        if (typeName.EndsWith("Definition")) typeName = typeName[..^"Definition".Length];

        return typeName + "s";
    }
}
