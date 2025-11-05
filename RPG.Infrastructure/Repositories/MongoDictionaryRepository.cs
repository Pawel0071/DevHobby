using MongoDB.Driver;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories;

public class MongoDictionaryRepository<T> : IDictionaryRepository<T>
{
    private readonly IMongoCollection<T> _collection;

    public MongoDictionaryRepository(IMongoDatabase database)
    {
        var collectionName = GetCollectionName(typeof(T));
        _collection = database.GetCollection<T>(collectionName);
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