using RPG.Domain.Common;

namespace RPG.Domain.Entities.Items;

public class Item(Guid itemId, string typeCode)
{
    public Guid Id { get; init; } = itemId;
    public string Name { get; set; } = null!;
    public ItemRarity Rarity { get; set; }
    public string TypeCode  { get; init; } = typeCode;
    public int RequiredLevel { get; set; }
    public int StackSize { get; set; }
    public HashSet<string> Tags { get; set; } = new HashSet<string>();
    public IList<IItemComponent> Components { get; set; } = new List<IItemComponent>();
    public T? GetComponent<T>() where T : class, IItemComponent
        => Components.OfType<T>().FirstOrDefault();
    
}