using MongoDB.Driver;
using RPG.Domain.Common;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.MongoDB;

public class MongoDictionaryRepository<T> : IDictionaryRepository<T>
{
    private readonly IMongoCollection<T> _collection;

    public MongoDictionaryRepository(IMongoDatabase database)
    {
        var collectionName = GetCollectionName(typeof(T));
        _collection = database.GetCollection<T>(collectionName);
    }

    // Added for easier unit testing - allow passing a collection directly
    public MongoDictionaryRepository(IMongoCollection<T> collection)
    {
        _collection = collection;
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await _collection.Find(_ => true).ToListAsync(cancellationToken);
        return result;
    }

    private static string GetCollectionName(Type type)
    {
        var name = type.Name;

        // Przykład: ItemTagDefinition → ItemTags
        if (name.EndsWith("Definition"))
            name = name[..^"Definition".Length];

        return name + "s";
    }
}