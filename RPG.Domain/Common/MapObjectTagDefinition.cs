namespace RPG.Domain.Common;

/// <summary>
///     Defines standard tags for map objects.
///     Tags are used for categorization, filtering, and gameplay mechanics.
/// </summary>
public static class MapObjectTagDefinition
{
    // Type tags
    public const string Container = "container";
    public const string Door = "door";
    public const string Trigger = "trigger";
    public const string CraftingStation = "crafting-station";
    public const string ResourceNode = "resource-node";
    public const string Furniture = "furniture";
    public const string Decoration = "decoration";
    public const string Trap = "trap";
    public const string Portal = "portal";
    public const string Shrine = "shrine";
    public const string Vendor = "vendor";
    public const string QuestObject = "quest-object";

    // Interaction tags
    public const string Interactable = "interactable";
    public const string NonInteractable = "non-interactable";
    public const string Locked = "locked";
    public const string Unlocked = "unlocked";
    public const string RequiresKey = "requires-key";
    public const string Lockpickable = "lockpickable";

    // State tags
    public const string Active = "active";
    public const string Inactive = "inactive";
    public const string Destroyed = "destroyed";
    public const string Harvested = "harvested";
    public const string Available = "available";
    public const string Open = "open";
    public const string Closed = "closed";

    // Crafting station tags
    public const string Blacksmith = "blacksmith";
    public const string Alchemy = "alchemy";
    public const string Enchanting = "enchanting";
    public const string Cooking = "cooking";
    public const string Leatherworking = "leatherworking";
    public const string Tailoring = "tailoring";
    public const string Woodworking = "woodworking";

    // Resource node tags
    public const string Mining = "mining";
    public const string Herbalism = "herbalism";
    public const string Fishing = "fishing";
    public const string Logging = "logging";
    public const string Skinning = "skinning";

    // Trigger tags
    public const string OnEnter = "on-enter";
    public const string OnExit = "on-exit";
    public const string OnInteract = "on-interact";
    public const string Proximity = "proximity";
    public const string Timed = "timed";
    public const string TriggerOnce = "trigger-once";

    // Size/Scale tags
    public const string Small = "small";
    public const string Medium = "medium";
    public const string Large = "large";
    public const string Huge = "huge";

    // Special tags
    public const string Destructible = "destructible";
    public const string Indestructible = "indestructible";
    public const string Respawnable = "respawnable";
    public const string Permanent = "permanent";
}
