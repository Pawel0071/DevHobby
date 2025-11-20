using System.Text.Json;
using System.Text.Json.Serialization;
using RPG.Domain.Enums;
using RPG.Domain.Models; // WorldState, Location
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Skills;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.MapObjects;

namespace RPG.WorldSeeder.Seeders;

// Shared location seed model
internal sealed class LocationSeedModel
{
    [JsonPropertyName("position")] public PositionSeedModel? Position { get; set; }
    [JsonPropertyName("mapId")] public string? MapId { get; set; }
    [JsonPropertyName("zoneName")] public string? ZoneName { get; set; }
    [JsonPropertyName("worldId")] public Guid WorldId { get; set; }
    [JsonPropertyName("rotation")] public float Rotation { get; set; }

    public Location ToDomain()
    {
        var loc = new Location
        {
            MapId = MapId ?? string.Empty,
            MapName = ZoneName ?? string.Empty,
            WorldId = WorldId,
            Direction = Rotation
        };
        if (Position is not null)
        {
            loc.Position = new System.Numerics.Vector3(Position.X, Position.Y, Position.Z);
        }
        return loc;
    }
}

internal sealed class PositionSeedModel
{
    [JsonPropertyName("x")] public float X { get; set; }
    [JsonPropertyName("y")] public float Y { get; set; }
    [JsonPropertyName("z")] public float Z { get; set; }
}

// Item seed
internal sealed class ItemSeedModel
{
    public Guid Id { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RequiredLevel { get; set; }
    public int StackSize { get; set; } = 1;
    public List<string>? Tags { get; set; }
    public string? Rarity { get; set; }
}

// Skill seed
internal sealed class SkillSeedModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconId { get; set; }
    public List<string>? Tags { get; set; }
    public List<SkillComponentSeedModel>? Components { get; set; }
}

internal sealed class SkillComponentSeedModel
{
    public string? Type { get; set; }
    public JsonElement Properties { get; set; }
}

// MapObject seed
internal sealed class MapObjectSeedModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public Guid WorldId { get; set; }
    public string? ZoneId { get; set; }
    public LocationSeedModel Location { get; set; } = new();
    public float RotationYaw { get; set; }
    public bool IsActive { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string,string>? State { get; set; }
    public DateTime? LastUpdated { get; set; }
}

// NPC seed
internal sealed class NpcSeedModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public int Level { get; set; }
    public Guid WorldId { get; set; }
    public LocationSeedModel SpawnLocation { get; set; } = new();
    public LocationSeedModel? CurrentLocation { get; set; }
    public List<string>? Tags { get; set; }
    public bool IsAlive { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? RespawnAt { get; set; }
    public List<NpcComponentSeedModel>? Components { get; set; }

    // Base character style fields
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentResource { get; set; }
    public int MaxResource { get; set; }

    public Dictionary<string,int>? BaseStats { get; set; }
    public Dictionary<string,int>? ModifiedStats { get; set; }

    // Skills directly on NPC
    public List<NpcSkillEntrySeedModel>? Skills { get; set; }
    public List<NpcActiveSkillEntrySeedModel>? ActiveSkills { get; set; }
}

internal sealed class NpcComponentSeedModel
{
    public string? Type { get; set; }
    public JsonElement Properties { get; set; }
}

internal sealed class NpcSkillEntrySeedModel
{
    public Guid SkillId { get; set; }
    public string Availability { get; set; } = string.Empty;
}

internal sealed class NpcActiveSkillEntrySeedModel
{
    public Guid SkillId { get; set; }
    public DateTime? LastUsed { get; set; }
}

// Combat component seed (after refactor no internal containers)
internal sealed class CombatComponentSeedModel
{
    public float AggroRange { get; set; }
    public float LeashRange { get; set; }
    public string? AiBehaviorScript { get; set; }
    public Dictionary<string,int>? Stats { get; set; }
    public List<NpcSkillEntrySeedModel>? Skills { get; set; }
}

internal sealed class TrainerComponentSeedModel
{
    public string? Specialization { get; set; }
    public List<NpcSkillEntrySeedModel>? TeachableSkills { get; set; }
}

internal sealed class LootableComponentSeedModel
{
    public int ExperienceReward { get; set; }
    public int GoldReward { get; set; }
    public List<LootEntrySeedModel>? LootTable { get; set; }
}

internal sealed class LootEntrySeedModel
{
    public int Slot { get; set; }
    public Guid ItemId { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public float DropChance { get; set; }
}

internal sealed class MerchantComponentSeedModel
{
    public int GoldAmount { get; set; }
    public float GlobalPriceModifier { get; set; }
    public Dictionary<string,float>? PriceModifiers { get; set; }
    public List<MerchantInventorySlotSeedModel>? Inventory { get; set; }
}

internal sealed class MerchantInventorySlotSeedModel
{
    public int Slot { get; set; }
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
}

internal sealed class RespawnComponentSeedModel
{
    public int RespawnTimeSeconds { get; set; }
    public LocationSeedModel? RespawnLocation { get; set; }
}

// World state seed (simplified)
internal sealed class WorldStateSeedModel
{
    public Guid Id { get; set; }
    public Guid WorldId { get; set; }
    public string WorldName { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }
    public List<Guid>? Characters { get; set; }
    public List<Guid>? Npcs { get; set; }
    public List<Guid>? MapObjects { get; set; }
}

// Aggregate seed set returned by loader
internal sealed class SeedDataSet
{
    public IReadOnlyDictionary<Guid, RPG.Domain.Models.Items.Item> Items { get; }
    public IReadOnlyDictionary<Guid, RPG.Domain.Models.Skills.Skill> Skills { get; }
    public IReadOnlyList<RPG.Domain.Models.Npcs.Npc> Npcs { get; }
    public IReadOnlyList<RPG.Domain.Models.MapObjects.MapObject> MapObjects { get; }
    public RPG.Domain.Models.WorldState WorldState { get; }

    public SeedDataSet(
        IReadOnlyDictionary<Guid, RPG.Domain.Models.Items.Item> items,
        IReadOnlyDictionary<Guid, RPG.Domain.Models.Skills.Skill> skills,
        IReadOnlyList<RPG.Domain.Models.Npcs.Npc> npcs,
        IReadOnlyList<RPG.Domain.Models.MapObjects.MapObject> mapObjects,
        RPG.Domain.Models.WorldState worldState)
    {
        Items = items;
        Skills = skills;
        Npcs = npcs;
        MapObjects = mapObjects;
        WorldState = worldState;
    }
}
