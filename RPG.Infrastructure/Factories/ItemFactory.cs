using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Infrastructure.Common;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Factories;

public class ItemFactory(IDictionaryRegistry<ItemTagDefinition> tagRegistry)
{
    public Item Create(ItemDocument doc, ItemTypeDefinition def)
    {
        var item = doc.ToDomain(def);

        foreach (var type in tagRegistry.GetRequiredComponents(item.Tags))
        {
            var component = ItemComponentFactory.Create(type, doc);
            if (component != null)
                item.Components.Add(component);
        }

        return item;
    }
}