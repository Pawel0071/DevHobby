using RPG.Domain.Common.Interfaces;

namespace RPG.Domain.Common;

/// <summary>
///     Defines valid NPC tags and their metadata.
///     Tags categorize NPCs and define their basic behavior types.
/// </summary>
public class NpcTagDefinition : IDictionaryEntry<NpcTagDefinition>
{
    private static readonly Dictionary<string, NpcTagDefinition> Registry = new()
    {
        // Disposition tags (exclusive - NPC can only be one of these)
        ["friendly"] =
            new NpcTagDefinition
            {
                Tag = "friendly",
                DisplayName = "Friendly",
                Description = "NPC is friendly and can be interacted with peacefully",
                IsExclusive = true,
                ExclusiveGroup = "disposition"
            },
        ["hostile"] =
            new NpcTagDefinition
            {
                Tag = "hostile",
                DisplayName = "Hostile",
                Description = "NPC is hostile and will attack on sight",
                IsExclusive = true,
                ExclusiveGroup = "disposition"
            },
        ["neutral"] = new NpcTagDefinition
        {
            Tag = "neutral",
            DisplayName = "Neutral",
            Description = "NPC is neutral and won't attack unless provoked",
            IsExclusive = true,
            ExclusiveGroup = "disposition"
        },

        // Role tags (non-exclusive - NPC can have multiple roles)
        ["merchant"] =
            new NpcTagDefinition
            {
                Tag = "merchant", DisplayName = "Merchant", Description = "NPC can buy and sell items"
            },
        ["quest-giver"] =
            new NpcTagDefinition
            {
                Tag = "quest-giver", DisplayName = "Quest Giver", Description = "NPC offers quests to players"
            },
        ["trainer"] =
            new NpcTagDefinition
            {
                Tag = "trainer", DisplayName = "Trainer", Description = "NPC can teach skills or abilities"
            },
        ["guard"] =
            new NpcTagDefinition
            {
                Tag = "guard", DisplayName = "Guard", Description = "NPC patrols and protects an area"
            },
        ["boss"] =
            new NpcTagDefinition
            {
                Tag = "boss",
                DisplayName = "Boss",
                Description = "Powerful enemy with unique mechanics and better loot"
            },
        ["elite"] =
            new NpcTagDefinition { Tag = "elite", DisplayName = "Elite", Description = "Stronger than normal enemies" },
        ["rare"] =
            new NpcTagDefinition
            {
                Tag = "rare", DisplayName = "Rare Spawn", Description = "Rarely spawning NPC with special rewards"
            },
        ["vendor"] =
            new NpcTagDefinition
            {
                Tag = "vendor", DisplayName = "Vendor", Description = "General merchant selling various goods"
            },
        ["banker"] =
            new NpcTagDefinition
            {
                Tag = "banker", DisplayName = "Banker", Description = "NPC provides banking services"
            },
        ["healer"] =
            new NpcTagDefinition
            {
                Tag = "healer", DisplayName = "Healer", Description = "NPC can restore health and remove debuffs"
            },
        ["mount-vendor"] =
            new NpcTagDefinition
            {
                Tag = "mount-vendor",
                DisplayName = "Mount Vendor",
                Description = "Sells mounts and mount-related items"
            },
        ["flight-master"] = new NpcTagDefinition
        {
            Tag = "flight-master", DisplayName = "Flight Master", Description = "Provides fast travel services"
        }
    };

    public required string Tag { get; init; }
    public required string DisplayName { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool IsExclusive { get; init; } // If true, NPC can only have one tag from exclusive group
    public string ExclusiveGroup { get; init; } = string.Empty;
    public string Code => Tag;

    public static IEnumerable<NpcTagDefinition> Predefined => Registry.Values;

    public static NpcTagDefinition? GetByTag(string tag)
    {
        return Registry.TryGetValue(tag.ToLower(), out var definition) ? definition : null;
    }

    public static IEnumerable<NpcTagDefinition> GetAll()
    {
        return Registry.Values;
    }

    public static bool IsValid(string tag)
    {
        return Registry.ContainsKey(tag.ToLower());
    }

    public static bool AreTagsCompatible(string tag1, string tag2)
    {
        var def1 = GetByTag(tag1);
        var def2 = GetByTag(tag2);

        if (def1 == null || def2 == null) return false;

        // If both are exclusive and in the same group, they're incompatible
        if (def1.IsExclusive && def2.IsExclusive &&
            def1.ExclusiveGroup == def2.ExclusiveGroup &&
            !string.IsNullOrEmpty(def1.ExclusiveGroup))
            return false;

        return true;
    }

    public static IEnumerable<NpcTagDefinition> GetByGroup(string group)
    {
        return Registry.Values.Where(d => d.ExclusiveGroup == group);
    }
}
