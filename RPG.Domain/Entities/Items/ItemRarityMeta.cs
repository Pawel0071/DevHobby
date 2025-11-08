namespace RPG.Domain.Entities.Items;

public static class ItemRarityMeta
{
    private static readonly Dictionary<ItemRarity, string> RarityColors = new()
    {
        [ItemRarity.Common] = "#AAAAAA",
        [ItemRarity.Uncommon] = "#33CC33",
        [ItemRarity.Rare] = "#3399FF",
        [ItemRarity.Epic] = "#9933FF",
        [ItemRarity.Legendary] = "#FF9900",
        [ItemRarity.Mythic] = "#FF3366",
        [ItemRarity.Unique] = "#FFD700"
    };

    public static string GetColor(ItemRarity rarity)
    {
        return RarityColors.TryGetValue(rarity, out var color) ? color : "#FFFFFF";
    }
}
