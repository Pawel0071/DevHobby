using MongoDB.Driver;
using RPG.Domain.Common;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories;

public class CachedItemRepository : IItemRepository
{
    private readonly IRedisCache _redis;
    private readonly IMongoCollection<ItemDocument> _mongo;
    private readonly IRabbitPublisher _rabbit;

    public CachedItemRepository(IRedisCache redis, IMongoCollection<ItemDocument> mongo, IRabbitPublisher rabbit)
    {
        _redis = redis;
        _mongo = mongo;
        _rabbit = rabbit;
    }

    public async Task<Item?> GetByIdAsync(Guid id)
    {
        var item = await TryGetFromCacheAsync(id);
        if (item != null)
            return item;

        item = await TryGetFromDatabaseAsync(id);
        if (item != null)
            await CacheItemAsync(item);

        return item;
    }

    public async Task<Item?> GetByNameAsync(string name)
    {
        // Redis nie indeksuje po nazwie — tylko Mongo
        var doc = await _mongo.Find(x => x.Name == name).FirstOrDefaultAsync();
        if (doc == null)
            return null;

        var item = doc.ToDomain();
        await CacheItemAsync(item);
        return item;
    }

    public async Task SaveAsync(Item item)
    {
        await CacheItemAsync(item);
        await PublishSaveAsync(item);
    }

    // 🔹 Pomocnicze metody

    public async Task<Item?> TryGetFromCacheAsync(Guid id)
    {
        return await _redis.GetAsync<Item>($"item:{id}");
    }

    public async Task<Item?> TryGetFromDatabaseAsync(Guid id)
    {
        var doc = await _mongo.Find(x => x.Id == id).FirstOrDefaultAsync();
        return doc?.ToDomain();
    }

    private async Task CacheItemAsync(Item item)
    {
        await _redis.SetAsync($"item:{item.Id}", item, TimeSpan.FromHours(1));
    }

    private async Task PublishSaveAsync(Item item)
    {
        await _rabbit.PublishAsync("item.save", item);
    }
}