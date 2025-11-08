using RPG.Domain.Common;
using RPG.Domain.Containers;

namespace RPG.Domain.Entities.Quests.QuestComponents;

/// <summary>
///     Component that defines item rewards.
///     Uses InventoryContainer like Character.
/// </summary>
public class ItemRewardsComponent : IQuestComponent
{
    private InventoryContainer GuaranteedItemsContainer { get; } = new(10);
    private InventoryContainer ChoiceItemsContainer { get; } = new(10);

    /// <summary>
    ///     Items automatically given upon quest completion (like Character's inventory)
    /// </summary>
    public IList<InventorySlot> GuaranteedItems => GuaranteedItemsContainer.Inventory;

    /// <summary>
    ///     Items player can choose from (e.g., choose 1 of 3)
    /// </summary>
    public IList<InventorySlot> ChoiceItems => ChoiceItemsContainer.Inventory;

    /// <summary>
    ///     Number of items player can choose
    /// </summary>
    public int ChoiceCount { get; set; } = 1;

    /// <summary>
    ///     Get the guaranteed items container (for services)
    /// </summary>
    public InventoryContainer GetGuaranteedItemsContainer()
    {
        return GuaranteedItemsContainer;
    }

    /// <summary>
    ///     Get the choice items container (for services)
    /// </summary>
    public InventoryContainer GetChoiceItemsContainer()
    {
        return ChoiceItemsContainer;
    }
}
