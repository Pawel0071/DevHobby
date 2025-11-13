using RPG.Domain.Common;
using RPG.Domain.Containers;

namespace RPG.Domain.Models.Npcs.NpcComponents;

/// <summary>
///     Component for NPCs that drop loot and give rewards when killed.
/// </summary>
public class LootableComponent : INpcComponent
{
    private LootContainer LootTableContainer { get; } = new(20); // Default 20 loot slots

    /// <summary>
    ///     Public access to loot slots (like Character's inventory)
    /// </summary>
    public IList<LootSlot> LootTable => LootTableContainer.LootSlots;

    /// <summary>
    ///     Experience reward for killing this NPC
    /// </summary>
    public int ExperienceReward { get; set; }

    /// <summary>
    ///     Gold reward for killing this NPC
    /// </summary>
    public int GoldReward { get; set; }

    /// <summary>
    ///     Get the loot container (for services that need full container interface)
    /// </summary>
    public LootContainer GetLootContainer()
    {
        return LootTableContainer;
    }
}
