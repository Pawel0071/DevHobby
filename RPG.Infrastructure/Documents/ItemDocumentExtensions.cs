using RPG.Domain.Common;
using RPG.Domain.Containers;
using RPG.Domain.Entities.Items;
using RPG.Infrastructure.Mappers;

namespace RPG.Infrastructure.Documents;

/// <summary>
/// Extension methods for ItemDocument - delegates to ItemDocumentMapper
/// </summary>
public static class ItemDocumentExtensions
{
    public static Item ToDomain(this ItemDocument doc, ItemTypeDefinition? def = null)
    {
        var mapper = new ItemDocumentMapper(def);
        return mapper.ToDomain(doc);
    }

    public static ItemDocument ToDocument(this Item item)
    {
        var mapper = new ItemDocumentMapper();
        return mapper.ToDocument(item);
    }
}
