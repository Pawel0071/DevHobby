using RPG.Domain.Common;

namespace RPG.Domain.Containers;

/// <summary>
///     Container for loot drops from NPCs.
///     Similar to InventoryContainer but for loot tables.
/// </summary>
public class LootContainer
{
    public LootContainer(int capacity)
    {
        Capacity = capacity;
        LootSlots = Enumerable.Range(0, capacity)
            .Select(_ => new LootSlot())
            .ToList();
    }

    public IList<LootSlot> LootSlots { get; set; }
    public int Capacity { get; init; }
}
