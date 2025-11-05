using MongoDB.Driver;
using RPG.Domain.Entities.Items;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Mango;

public class ItemSaveMangoConsumer : IMangoConsumer<Item>
{
    private readonly IMongoCollection<ItemDocument> _mongo;
    private readonly ILogger<ItemSaveMangoConsumer> _logger;

    public ItemSaveMangoConsumer(
        IMongoCollection<ItemDocument> mongo,
        ILogger<ItemSaveMangoConsumer> logger)
    {
        _mongo = mongo;
        _logger = logger;
    }

    public async Task Consume(Item item)
    {
        try
        {
            _logger.Debug($"Saving item {item.Id} ({item.Name}) to MongoDB.");
            var doc = item.ToDocument();
            await _mongo.ReplaceOneAsync(x => x.Id == item.Id, doc, new ReplaceOptions { IsUpsert = true });
            _logger.Info($"Item {item.Id} saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save item {item.Id} to MongoDB.", ex);
            throw;
        }
    }
}