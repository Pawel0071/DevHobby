namespace RPG.Domain.Models.Items.ItemComponent;

public class CraftMaterialComponent : IItemComponent
{
    public IList<string> UsedInItemIds { get; init; } = new List<string>();
}
