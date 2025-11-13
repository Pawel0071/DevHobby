using RPG.Domain.Common;
using RPG.Domain.Containers;

namespace RPG.Domain.Models.Quests.QuestComponents;

/// <summary>
///     Component for quests that require delivering items to an NPC.
/// </summary>
public class DeliverObjectiveComponent : IQuestComponent
{
    private InventoryContainer ItemsToDeliverContainer { get; } = new(10);

    /// <summary>
    ///     Items that need to be delivered (Item + required Quantity in InventorySlot)
    /// </summary>
    public IList<InventorySlot> ItemsToDeliver => ItemsToDeliverContainer.Inventory;

    public Guid DeliverToNpcId { get; set; }
    public string DeliverToNpcName { get; set; } = string.Empty;

    /// <summary>
    ///     Get the items to deliver container (for services)
    /// </summary>
    public InventoryContainer GetItemsToDeliverContainer()
    {
        return ItemsToDeliverContainer;
    }
}
