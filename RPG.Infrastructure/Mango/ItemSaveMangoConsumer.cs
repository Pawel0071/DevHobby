using MongoDB.Driver;
using RPG.Domain.Common;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Mango;

public class ItemSaveMangoConsumer : IMangoConsumer<Item>
{
    private readonly IMongoCollection<ItemDocument> _mongo;

    public ItemSaveMangoConsumer(IMongoCollection<ItemDocument> mongo)
    {
        _mongo = mongo;
    }

    public async Task Consume(Item item)
    {
        var doc = ItemDocument.FromDomain(item);
        await _mongo.ReplaceOneAsync(x => x.Id == item.Id, doc, new ReplaceOptions { IsUpsert = true });
    }
}