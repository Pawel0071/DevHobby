using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Infrastructure.Common;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Factories;

public class ItemFactory
{
    private readonly IDictionaryRegistry<ItemTagDefinition> _tagRegistry;
    private readonly ILogger<ItemFactory> _logger;

    public ItemFactory(IDictionaryRegistry<ItemTagDefinition> tagRegistry, ILogger<ItemFactory> logger)
    {
        _tagRegistry = tagRegistry;
        _logger = logger;
    }

    public Item Create(ItemDocument doc, ItemTypeDefinition def)
    {
        _logger.Debug($"Creating item from document. Id={doc.Id}, Type={doc.TypeCode}");
        
        var item = doc.ToDomain(def);

        foreach (var type in _tagRegistry.GetRequiredComponents(item.Tags))
        {
            var component = ItemComponentFactory.Create(type, doc);
            if (component != null)
            {
                item.Components.Add(component);
                _logger.Debug($"Added component {type.Name} to item {item.Id}");
            }
        }

        _logger.Debug($"Item created successfully. Id={item.Id}, Components={item.Components.Count}");
        return item;
    }
}