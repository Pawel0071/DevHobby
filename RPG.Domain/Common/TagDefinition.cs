using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPG.Domain.Common.Interfaces;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Domain.Entities.MapObjects.MapObjectComponents;
using RPG.Domain.Entities.Npcs.NpcComponents;
using RPG.Domain.Entities.Quests.QuestComponents;
using RPG.Domain.Entities.Skills.SkillComponents;
using RPG.Domain.Enums;

namespace RPG.Domain.Common;

public sealed class TagDefinition : IDictionaryEntry<TagDefinition>
{
    private static readonly TagDefinition[] BuiltInDefinitions = BuildDefinitions();

    public string? DisplayName { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }
    public required string Code { get; init; }
    public TagTarget Target { get; init; } = TagTarget.Item;
    public string? ComponentType { get; init; }
    public bool IsExclusive { get; init; }
    public string? ExclusiveGroup { get; init; }

    public static IEnumerable<TagDefinition> Predefined => BuiltInDefinitions;

    public Type? ResolveComponentType()
    {
        return string.IsNullOrWhiteSpace(ComponentType)
            ? null
            : Type.GetType(ComponentType, throwOnError: false, ignoreCase: false);
    }

    private static TagDefinition[] BuildDefinitions()
    {
        var definitions = new List<TagDefinition>();
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(TagDefinition definition)
        {
            if (seenCodes.Add(definition.Code))
            {
                definitions.Add(definition);
            }
        }

        foreach (var definition in BuildComponentDefinitions())
        {
            Add(definition);
        }

        foreach (var definition in BuildSkillTagDefinitions())
        {
            Add(definition);
        }

        foreach (var definition in BuildMapObjectTagDefinitions())
        {
            Add(definition);
        }

        foreach (var definition in BuildNpcTagDefinitions())
        {
            Add(definition);
        }

        foreach (var definition in BuildQuestTagDefinitions())
        {
            Add(definition);
        }

        return definitions.ToArray();
    }

    private static IEnumerable<TagDefinition> BuildComponentDefinitions()
    {
        return new[]
        {
            CreateComponent<EquippableComponent>("item:equippable", TagTarget.Item, "Equippable Item", "Item",
                "Marks items that can be equipped into character slots."),
            CreateComponent<StatsComponent>("item:stats", TagTarget.Item, "Stat Modifiers", "Item",
                "Provides numerical stat bonuses when equipped or consumed."),
            CreateComponent<SkillGrantComponent>("item:grants-skill", TagTarget.Item, "Grants Skill", "Item",
                "Unlocks or grants access to skills when the item is used."),
            CreateComponent<SocketComponent>("item:socketable", TagTarget.Item, "Socketed Item", "Item",
                "Provides socket slots that can accept additional upgrades."),
            CreateComponent<CraftMaterialComponent>("item:material", TagTarget.Item, "Crafting Material", "Item",
                "Identifies items that are ingredients for crafting recipes."),
            CreateComponent<QuestItemComponent>("item:quest", TagTarget.Item, "Quest Item", "Item",
                "Special items tied to quest progress or objectives."),

            CreateComponent<DamageComponent>("skill:damage", TagTarget.Skill, "Direct Damage", "Skill",
                "Executes instant damage when the skill is used."),
            CreateComponent<DamageOverTimeComponent>("skill:damage-over-time", TagTarget.Skill, "Damage Over Time",
                "Skill", "Applies lingering damage across multiple ticks."),
            CreateComponent<HealingComponent>("skill:healing", TagTarget.Skill, "Direct Healing", "Skill",
                "Restores health instantly upon cast."),
            CreateComponent<HealOverTimeComponent>("skill:heal-over-time", TagTarget.Skill, "Heal Over Time",
                "Skill", "Restores health gradually over a duration."),
            CreateComponent<BuffComponent>("skill:buff", TagTarget.Skill, "Buff", "Skill",
                "Provides positive status effects to the target."),
            CreateComponent<DebuffComponent>("skill:debuff", TagTarget.Skill, "Debuff", "Skill",
                "Applies negative status effects to hostile targets."),
            CreateComponent<ShieldComponent>("skill:shield", TagTarget.Skill, "Shield", "Skill",
                "Generates a protective barrier that absorbs incoming damage."),
            CreateComponent<CrowdControlComponent>("skill:crowd-control", TagTarget.Skill, "Crowd Control", "Skill",
                "Restricts movement or actions of affected entities."),
            CreateComponent<AreaOfEffectComponent>("skill:area-of-effect", TagTarget.Skill, "Area of Effect", "Skill",
                "Impacts multiple targets within a specified radius."),
            CreateComponent<MovementComponent>("skill:movement", TagTarget.Skill, "Movement", "Skill",
                "Modifies position or mobility when activated."),
            CreateComponent<CastingComponent>("skill:casting", TagTarget.Skill, "Casting", "Skill",
                "Configures cast-time requirements before activation."),
            CreateComponent<CooldownComponent>("skill:cooldown", TagTarget.Skill, "Cooldown", "Skill",
                "Controls cooldown behaviour and charge recovery."),
            CreateComponent<ComboComponent>("skill:combo", TagTarget.Skill, "Combo", "Skill",
                "Defines chained ability interactions and follow-ups."),
            CreateComponent<RequirementComponent>("skill:requirements", TagTarget.Skill, "Requirements", "Skill",
                "Lists prerequisites needed before the skill can be used."),
            CreateComponent<ResourceCostComponent>("skill:resource-cost", TagTarget.Skill, "Resource Cost", "Skill",
                "Consumes or generates resources when the skill is cast."),

            CreateComponent<ContainerComponent>("map:container", TagTarget.MapObject, "Container", "Map Object",
                "Allows the map object to hold items within an inventory."),
            CreateComponent<LockableComponent>("map:lockable", TagTarget.MapObject, "Lockable", "Map Object",
                "Adds lock state that can require keys or lockpicking."),
            CreateComponent<DoorComponent>("map:door", TagTarget.MapObject, "Door", "Map Object",
                "Enables door mechanics such as open, close, and transitions."),
            CreateComponent<TriggerComponent>("map:trigger", TagTarget.MapObject, "Trigger", "Map Object",
                "Executes scripted interactions when activated."),
            CreateComponent<PortalComponent>("map:portal", TagTarget.MapObject, "Portal", "Map Object",
                "Teleports players to other locations or worlds."),
            CreateComponent<InteractionComponent>("map:interaction", TagTarget.MapObject, "Interaction", "Map Object",
                "Provides context-sensitive interactions for the object."),
            CreateComponent<CraftingStationComponent>("map:crafting-station", TagTarget.MapObject, "Crafting Station",
                "Map Object", "Allows players to craft or refine items at the location."),
            CreateComponent<ResourceNodeComponent>("map:resource-node", TagTarget.MapObject, "Resource Node",
                "Map Object", "Defines harvestable nodes that yield crafting resources."),
            CreateComponent<DestructibleComponent>("map:destructible", TagTarget.MapObject, "Destructible",
                "Map Object", "Allows the object to take damage and be destroyed."),

            CreateComponent<CombatComponent>("npc:combat", TagTarget.Npc, "Combat", "NPC",
                "Enables combat behaviour for the NPC."),
            CreateComponent<DialogueComponent>("npc:dialogue", TagTarget.Npc, "Dialogue", "NPC",
                "Provides dialogue scripts and conversation options."),
            CreateComponent<LootableComponent>("npc:lootable", TagTarget.Npc, "Lootable", "NPC",
                "Defines loot tables that drop when the NPC is defeated."),
            CreateComponent<MerchantComponent>("npc:merchant", TagTarget.Npc, "Merchant", "NPC",
                "Allows trading and vendor inventory interactions."),
            CreateComponent<QuestGiverComponent>("npc:quest-giver", TagTarget.Npc, "Quest Giver", "NPC",
                "Enables offering and tracking quests for players."),
            CreateComponent<RespawnComponent>("npc:respawn", TagTarget.Npc, "Respawn", "NPC",
                "Controls respawn timing and location for NPCs."),
            CreateComponent<TrainerComponent>("npc:trainer", TagTarget.Npc, "Trainer", "NPC",
                "Provides skill or profession training services."),

            CreateComponent<KillObjectiveComponent>("quest:kill-objective", TagTarget.Quest, "Kill Objective", "Quest",
                "Requires defeating one or more specific enemies."),
            CreateComponent<CollectObjectiveComponent>("quest:collect-objective", TagTarget.Quest, "Collect Objective",
                "Quest", "Requires gathering items or resources."),
            CreateComponent<DeliverObjectiveComponent>("quest:deliver-objective", TagTarget.Quest, "Deliver Objective",
                "Quest", "Tasked with delivering items or messages."),
            CreateComponent<ExploreObjectiveComponent>("quest:explore-objective", TagTarget.Quest, "Explore Objective",
                "Quest", "Requires visiting or discovering specific locations."),
            CreateComponent<InteractObjectiveComponent>("quest:interact-objective", TagTarget.Quest, "Interact Objective",
                "Quest", "Completes by interacting with specified world objects."),
            CreateComponent<LevelRequirementComponent>("quest:level-requirement", TagTarget.Quest, "Level Requirement",
                "Quest", "Sets minimum or maximum character level restrictions."),
            CreateComponent<ClassRequirementComponent>("quest:class-requirement", TagTarget.Quest, "Class Requirement",
                "Quest", "Restricts quest participation to specific classes."),
            CreateComponent<PrerequisiteQuestsComponent>("quest:prerequisite", TagTarget.Quest, "Prerequisite Quests",
                "Quest", "Requires completion of listed quests beforehand."),
            CreateComponent<QuestChainComponent>("quest:chain", TagTarget.Quest, "Quest Chain", "Quest",
                "Links quests together to form multi-step story arcs."),
            CreateComponent<RepeatableQuestComponent>("quest:repeatable", TagTarget.Quest, "Repeatable", "Quest",
                "Allows the quest to be repeated after completion."),
            CreateComponent<TimeLimitComponent>("quest:time-limit", TagTarget.Quest, "Time Limit", "Quest",
                "Requires completion within the specified time window."),
            CreateComponent<BasicRewardsComponent>("quest:basic-rewards", TagTarget.Quest, "Basic Rewards", "Quest",
                "Provides core rewards such as experience or currency."),
            CreateComponent<ItemRewardsComponent>("quest:item-rewards", TagTarget.Quest, "Item Rewards", "Quest",
                "Grants specific items upon quest completion."),
            CreateComponent<SkillRewardsComponent>("quest:skill-rewards", TagTarget.Quest, "Skill Rewards", "Quest",
                "Awards new skills or abilities when completed."),
            CreateComponent<ReputationRewardsComponent>("quest:reputation-rewards", TagTarget.Quest, "Reputation Rewards",
                "Quest", "Increases faction or reputation standings.")
        };
    }

    private static IEnumerable<TagDefinition> BuildSkillTagDefinitions()
    {
        var definitions = new List<TagDefinition>();

        AddSimpleRange(TagTarget.Skill, "Skill / Type",
        [
            "offensive",
            "defensive",
            "utility",
            "passive",
            "active",
            "toggle",
            "channeled"
        ]);

        AddSimpleRange(TagTarget.Skill, "Skill / Target",
        [
            "single-target",
            "area-of-effect",
            "self-only",
            "no-target",
            "ground-target",
            "cone",
            "line",
            "circle"
        ]);

        AddSimpleRange(TagTarget.Skill, "Skill / Damage Type",
        [
            "physical",
            "magical",
            "fire",
            "ice",
            "lightning",
            "poison",
            "holy",
            "shadow",
            "nature"
        ]);

        AddSimpleRange(TagTarget.Skill, "Skill / Effect",
        [
            "damage",
            "healing",
            "buff",
            "debuff",
            "stun",
            "slow",
            "root",
            "silence",
            "disarm",
            "blind",
            "fear",
            "taunt",
            "shield",
            "damage-over-time",
            "heal-over-time"
        ]);

        AddSimpleRange(TagTarget.Skill, "Skill / Movement",
        [
            "requires-standing",
            "cast-while-moving",
            "immobilizes",
            "teleport",
            "dash",
            "knockback",
            "pull"
        ]);

        AddSimpleRange(TagTarget.Skill, "Skill / Resource",
        [
            "costs-mana",
            "costs-health",
            "costs-energy",
            "costs-rage",
            "generates-resource",
            "no-resource-cost"
        ]);

        AddSimpleRange(TagTarget.Skill, "Skill / Cooldown",
        [
            "short-cooldown",
            "medium-cooldown",
            "long-cooldown",
            "no-cooldown",
            "global-cooldown"
        ]);

        AddSimpleRange(TagTarget.Skill, "Skill / Special",
        [
            "interruptible",
            "uninterruptible",
            "requires-weapon",
            "requires-melee",
            "requires-ranged",
            "ultimate",
            "combo",
            "chain",
            "stackable",
            "dispellable",
            "cleansable"
        ]);

        var classRequirementTags = Enum.GetNames(typeof(CharacterClass))
            .Select(name => $"class-{name.ToLowerInvariant()}")
            .ToArray();

        AddSimpleRange(TagTarget.Skill, "Skill / Class Requirement", classRequirementTags);

        AddSimpleRange(TagTarget.Skill, "Skill / Weapon Requirement",
        [
            "weapon-sword",
            "weapon-axe",
            "weapon-mace",
            "weapon-dagger",
            "weapon-spear",
            "weapon-staff",
            "weapon-bow",
            "weapon-crossbow",
            "weapon-gun",
            "weapon-1h",
            "weapon-2h"
        ]);

        AddSimpleRange(TagTarget.Skill, "Skill / Resource Requirement",
        [
            "resource-mana",
            "resource-energy",
            "resource-rage",
            "resource-focus"
        ]);

        return definitions;

        void AddSimpleRange(TagTarget target, string category, IEnumerable<string> suffixes)
        {
            foreach (var suffix in suffixes)
            {
                definitions.Add(CreateSimpleFromSuffix(target, suffix, category));
            }
        }
    }

    private static IEnumerable<TagDefinition> BuildMapObjectTagDefinitions()
    {
        var definitions = new List<TagDefinition>();

        AddSimpleRange(TagTarget.MapObject, "Map Object / Type",
        [
            "container",
            "door",
            "trigger",
            "crafting-station",
            "resource-node",
            "furniture",
            "decoration",
            "trap",
            "portal",
            "shrine",
            "vendor",
            "quest-object"
        ]);

        AddSimpleRange(TagTarget.MapObject, "Map Object / Interaction",
        [
            "interactable",
            "non-interactable",
            "locked",
            "unlocked",
            "requires-key",
            "lockpickable"
        ]);

        AddSimpleRange(TagTarget.MapObject, "Map Object / State",
        [
            "active",
            "inactive",
            "destroyed",
            "harvested",
            "available",
            "open",
            "closed"
        ]);

        AddSimpleRange(TagTarget.MapObject, "Map Object / Crafting",
        [
            "blacksmith",
            "alchemy",
            "enchanting",
            "cooking",
            "leatherworking",
            "tailoring",
            "woodworking"
        ]);

        AddSimpleRange(TagTarget.MapObject, "Map Object / Resource",
        [
            "mining",
            "herbalism",
            "fishing",
            "logging",
            "skinning"
        ]);

        AddSimpleRange(TagTarget.MapObject, "Map Object / Trigger",
        [
            "on-enter",
            "on-exit",
            "on-interact",
            "proximity",
            "timed",
            "trigger-once"
        ]);

        AddSimpleRange(TagTarget.MapObject, "Map Object / Size",
        [
            "small",
            "medium",
            "large",
            "huge"
        ]);

        AddSimpleRange(TagTarget.MapObject, "Map Object / Special",
        [
            "destructible",
            "indestructible",
            "respawnable",
            "permanent"
        ]);

        return definitions;

        void AddSimpleRange(TagTarget target, string category, IEnumerable<string> suffixes)
        {
            foreach (var suffix in suffixes)
            {
                definitions.Add(CreateSimpleFromSuffix(target, suffix, category));
            }
        }
    }

    private static IEnumerable<TagDefinition> BuildNpcTagDefinitions()
    {
        return new[]
        {
            CreateSimple("npc:friendly", TagTarget.Npc, "NPC / Disposition", "Friendly",
                "NPC is friendly and can be interacted with peacefully", true, "npc:disposition"),
            CreateSimple("npc:hostile", TagTarget.Npc, "NPC / Disposition", "Hostile",
                "NPC is hostile and will attack on sight", true, "npc:disposition"),
            CreateSimple("npc:neutral", TagTarget.Npc, "NPC / Disposition", "Neutral",
                "NPC is neutral and will not attack unless provoked", true, "npc:disposition"),

            CreateSimple("npc:merchant", TagTarget.Npc, "NPC / Role", "Merchant",
                "NPC can buy and sell items"),
            CreateSimple("npc:quest-giver", TagTarget.Npc, "NPC / Role", "Quest Giver",
                "NPC offers quests to players"),
            CreateSimple("npc:trainer", TagTarget.Npc, "NPC / Role", "Trainer",
                "NPC can teach skills or abilities"),
            CreateSimple("npc:guard", TagTarget.Npc, "NPC / Role", "Guard",
                "NPC patrols and protects an area"),
            CreateSimple("npc:boss", TagTarget.Npc, "NPC / Role", "Boss",
                "Powerful enemy with unique mechanics and better loot"),
            CreateSimple("npc:elite", TagTarget.Npc, "NPC / Role", "Elite",
                "Stronger than normal enemies"),
            CreateSimple("npc:rare", TagTarget.Npc, "NPC / Role", "Rare Spawn",
                "Rarely spawning NPC with special rewards"),
            CreateSimple("npc:vendor", TagTarget.Npc, "NPC / Role", "Vendor",
                "General merchant selling various goods"),
            CreateSimple("npc:banker", TagTarget.Npc, "NPC / Role", "Banker",
                "NPC provides banking services"),
            CreateSimple("npc:healer", TagTarget.Npc, "NPC / Role", "Healer",
                "NPC can restore health and remove debuffs"),
            CreateSimple("npc:mount-vendor", TagTarget.Npc, "NPC / Role", "Mount Vendor",
                "Sells mounts and mount-related items"),
            CreateSimple("npc:flight-master", TagTarget.Npc, "NPC / Role", "Flight Master",
                "Provides fast travel services")
        };
    }

    private static IEnumerable<TagDefinition> BuildQuestTagDefinitions()
    {
        var definitions = new List<TagDefinition>();

        AddSimpleRange(TagTarget.Quest, "Quest / Type",
        [
            "main",
            "side",
            "daily",
            "weekly",
            "event",
            "world",
            "dungeon",
            "raid"
        ]);

        AddSimpleRange(TagTarget.Quest, "Quest / Difficulty",
        [
            "trivial",
            "easy",
            "normal",
            "hard",
            "elite",
            "legendary"
        ], isExclusive: true, exclusiveGroup: "quest:difficulty");

        AddSimpleRange(TagTarget.Quest, "Quest / Category",
        [
            "combat",
            "exploration",
            "crafting",
            "social",
            "collection"
        ]);

        return definitions;

        void AddSimpleRange(TagTarget target, string category, IEnumerable<string> suffixes, bool isExclusive = false,
            string? exclusiveGroup = null)
        {
            foreach (var suffix in suffixes)
            {
                definitions.Add(CreateSimpleFromSuffix(target, suffix, category, isExclusive: isExclusive,
                    exclusiveGroup: exclusiveGroup));
            }
        }
    }

    private static TagDefinition CreateComponent<TComponent>(
        string code,
        TagTarget target,
        string displayName,
        string category,
        string description)
    {
        return new TagDefinition
        {
            Code = code,
            Target = target,
            DisplayName = displayName,
            Category = category,
            Description = description,
            ComponentType = typeof(TComponent).AssemblyQualifiedName ?? typeof(TComponent).FullName
        };
    }

    private static TagDefinition CreateSimpleFromSuffix(
        TagTarget target,
        string suffix,
        string category,
        string? displayName = null,
        string? description = null,
        bool isExclusive = false,
        string? exclusiveGroup = null)
    {
        var code = $"{GetPrefix(target)}:{suffix}";
        return CreateSimple(code, target, category, displayName, description, isExclusive, exclusiveGroup);
    }

    private static TagDefinition CreateSimple(
        string code,
        TagTarget target,
        string category,
        string? displayName = null,
        string? description = null,
        bool isExclusive = false,
        string? exclusiveGroup = null)
    {
        return new TagDefinition
        {
            Code = code,
            Target = target,
            Category = category,
            DisplayName = displayName ?? ToDisplayName(code),
            Description = description,
            IsExclusive = isExclusive,
            ExclusiveGroup = exclusiveGroup
        };
    }

    private static string GetPrefix(TagTarget target)
    {
        return target switch
        {
            TagTarget.Item => "item",
            TagTarget.Skill => "skill",
            TagTarget.Quest => "quest",
            TagTarget.Npc => "npc",
            TagTarget.MapObject => "map",
            _ => "tag"
        };
    }

    private static string ToDisplayName(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        var lastSegmentIndex = code.LastIndexOf(':');
        var segment = lastSegmentIndex >= 0 ? code[(lastSegmentIndex + 1)..] : code;
        segment = segment.Replace('-', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(segment);
    }
}
