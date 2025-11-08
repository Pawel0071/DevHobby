namespace RPG.Domain.Common;

/// <summary>
///     Defines valid quest tags and their metadata.
///     Similar to ItemTagDefinition and NpcTagDefinition.
/// </summary>
public static class QuestTagDefinition
{
    // Quest types
    public const string Main = "main";
    public const string Side = "side";
    public const string Daily = "daily";
    public const string Weekly = "weekly";
    public const string Event = "event";
    public const string World = "world";
    public const string Dungeon = "dungeon";
    public const string Raid = "raid";

    // Difficulty
    public const string Trivial = "trivial";
    public const string Easy = "easy";
    public const string Normal = "normal";
    public const string Hard = "hard";
    public const string Elite = "elite";
    public const string Legendary = "legendary";

    // Categories
    public const string Combat = "combat";
    public const string Exploration = "exploration";
    public const string Crafting = "crafting";
    public const string Social = "social";
    public const string Collection = "collection";

    /// <summary>
    ///     Quest type tags (mutually exclusive)
    /// </summary>
    public static readonly HashSet<string> QuestTypes = new()
    {
        Main,
        Side,
        Daily,
        Weekly,
        Event,
        World,
        Dungeon,
        Raid
    };

    /// <summary>
    ///     Difficulty tags (mutually exclusive)
    /// </summary>
    public static readonly HashSet<string> DifficultyTags = new()
    {
        Trivial,
        Easy,
        Normal,
        Hard,
        Elite,
        Legendary
    };

    /// <summary>
    ///     Category tags (can have multiple)
    /// </summary>
    public static readonly HashSet<string> CategoryTags = new()
    {
        Combat,
        Exploration,
        Crafting,
        Social,
        Collection
    };

    /// <summary>
    ///     All valid quest tags
    /// </summary>
    public static readonly HashSet<string> AllTags = new HashSet<string>()
        .Union(QuestTypes)
        .Union(DifficultyTags)
        .Union(CategoryTags)
        .ToHashSet();

    public static bool IsValid(string tag)
    {
        return AllTags.Contains(tag);
    }

    public static bool AreTagsCompatible(HashSet<string> tags)
    {
        var typeCount = tags.Count(t => QuestTypes.Contains(t));
        var difficultyCount = tags.Count(t => DifficultyTags.Contains(t));

        // Can only have one quest type and one difficulty
        return typeCount <= 1 && difficultyCount <= 1;
    }
}
