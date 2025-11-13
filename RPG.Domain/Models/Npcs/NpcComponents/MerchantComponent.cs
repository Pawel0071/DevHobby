using RPG.Domain.Common;
using RPG.Domain.Containers;

namespace RPG.Domain.Models.Npcs.NpcComponents;

/// <summary>
///     Component for NPCs that can trade items (merchants).
/// </summary>
public class MerchantComponent : INpcComponent
{
    private InventoryContainer MerchantContainer { get; } = new(20);
    public IList<InventorySlot> MerchantInventory => MerchantContainer.Inventory;
    public Dictionary<string, float> PriceModifiers { get; set; } = new();
    public float GlobalPriceModifier { get; set; }
    public int GoldAmount { get; set; }
}
