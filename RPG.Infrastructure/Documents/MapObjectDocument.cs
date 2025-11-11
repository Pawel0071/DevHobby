using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Documents;

/// <summary>
///     MongoDB document representing an interactive object in the game world.
///     Uses tags and components (stored as JSON strings) for flexibility.
/// </summary>
public class MapObjectDocument : IMongoDocument
{
    public static string CollectionName => "MapObjects";

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public required string Name { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Location & Positioning
    public LocationData Location { get; set; } = new();
    public float RotationYaw { get; set; }

    [BsonRepresentation(BsonType.String)] public Guid WorldId { get; set; }

    public string ZoneId { get; set; } = string.Empty;

    // State
    public bool IsActive { get; set; } = true;

    // Tags for categorization (e.g., "container", "locked", "door")
    public List<string> Tags { get; set; } = new();

    // Components stored as JSON strings (will be deserialized by mapper)
    // Each component is serialized to JSON for flexible storage
    public List<ComponentData> Components { get; set; } = new();

    // Arbitrary map object state persisted for the client snapshot
    public Dictionary<string, string> State { get; set; } = new();

    public DateTime LastUpdated { get; set; }
}

/// <summary>
///     Component data stored in MongoDB - type name + JSON payload
/// </summary>
public class ComponentData
{
    public string Type { get; set; } = string.Empty; // Component type name
    public string Data { get; set; } = string.Empty; // JSON serialized component
}
