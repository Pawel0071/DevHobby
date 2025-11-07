using MongoDB.Driver;
using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Common;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.Redis;

public class CachedItemRepository : IItemRepository
{
    private readonly IRedisCache _redis;
    private readonly IMongoCollection<ItemDocument> _mongo;
    private readonly IRabbitPublisher _rabbit;
    private readonly ILogger<CachedItemRepository> _logger;

    public CachedItemRepository(
        IRedisCache redis, 
        IMongoCollection<ItemDocument> mongo, 
        IRabbitPublisher rabbit,
        ILogger<CachedItemRepository> logger)
    {
        _redis = redis;
        _mongo = mongo;
        _rabbit = rabbit;
        _logger = logger;
    }

    public async Task<Item?> GetByIdAsync(Guid id)
    {
        var item = await TryGetFromCacheAsync(id);
        if (item != null)
        {
            _logger.Debug($"Item {id} found in cache");
            return item;
        }

        item = await TryGetFromDatabaseAsync(id);
        if (item != null)
        {
            _logger.Debug($"Item {id} found in database, caching");
            await CacheItemAsync(item);
        }
        else
        {
            _logger.Debug($"Item {id} not found");
        }

        return item;
    }

    public async Task<Item?> GetByNameAsync(string name)
    {
        _logger.Debug($"Getting item by name: {name}");
        // Redis nie indeksuje po nazwie — tylko Mongo
        var doc = await _mongo.Find(x => x.Name == name).FirstOrDefaultAsync();
        if (doc == null)
        {
            _logger.Debug($"Item with name {name} not found");
            return null;
        }

        var typeDefinition = await _redis.GetAsync<ItemTypeDefinition>($"itemTypeDefinition:{doc.TypeCode}");
        var item = doc.ToDomain(typeDefinition);
        await CacheItemAsync(item);
        return item;
    }

    public async Task SaveAsync(Item item)
    {
        _logger.Info($"Saving item {item.Id} ({item.Name})");
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
        var typeDefinition = await _redis.GetAsync<ItemTypeDefinition>($"itemTypeDefinition:{doc.TypeCode}");
        return doc?.ToDomain(typeDefinition);
    }

    private async Task CacheItemAsync(Item item)
    {
        var key = CacheKeyBuilder.Item(item.Id.ToString());
        _logger.Debug($"Caching item: {key}");
        await _redis.SetAsync(key, item, TimeSpan.FromHours(1));
    }

    private async Task PublishSaveAsync(Item item)
    {
        _logger.Debug($"Publishing item save event: {item.Id}");
        await _rabbit.PublishAsync("item.save", item);
    }
}