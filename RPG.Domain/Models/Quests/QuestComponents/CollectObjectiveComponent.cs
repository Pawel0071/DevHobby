using RPG.Domain.Common;
using RPG.Domain.Containers;

namespace RPG.Domain.Models.Quests.QuestComponents;

/// <summary>
///     Component for quests that require collecting items.
///     Stores which items to collect with required quantities.
/// </summary>
public class CollectObjectiveComponent : IQuestComponent
{
    private InventoryContainer RequiredItemsContainer { get; } = new(10);

    /// <summary>
    ///     Items that need to be collected (Item + required Quantity in InventorySlot)
    /// </summary>
    public IList<InventorySlot> RequiredItems => RequiredItemsContainer.Inventory;

    /// <summary>
    ///     Get the required items container (for services)
    /// </summary>
    public InventoryContainer GetRequiredItemsContainer()
    {
        return RequiredItemsContainer;
    }
}
