namespace RPG.Domain.Entities.Items.ItemComponent;

public class CraftMaterialComponent : IItemComponent
{
    public IList<string> UsedInItemIds { get; init; } = [];
}