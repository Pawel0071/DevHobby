using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Documents;

/// <summary>
///     Shared types and common classes used across multiple document models.
///     Consolidated here to avoid duplication.
/// </summary>
/// <summary>
///     Common location data used across multiple documents.
///     Stores Vector3 position as separate X, Y, Z for MongoDB compatibility.
/// </summary>
public class LocationData
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public string? WorldId { get; set; } // Guid as string for MongoDB
    public string MapId { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public float Rotation { get; set; }
}

/// <summary>
///     Inventory slot with item reference and quantity.
/// </summary>
public class InventorySlot
{
    [BsonRepresentation(BsonType.String)] public Guid ItemId { get; set; }

    public int Quantity { get; set; }
    public int Slot { get; set; }
}

/// <summary>
///     Loot table entry with drop chance and quantity.
/// </summary>
public class LootEntry
{
    [BsonRepresentation(BsonType.String)] public Guid ItemId { get; set; }

    public float DropChance { get; set; } // 0.0 - 1.0
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
}
