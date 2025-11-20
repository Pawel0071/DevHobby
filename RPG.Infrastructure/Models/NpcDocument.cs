using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Models;

public class NpcDocument : IPersistenceModel
{
    public static string CollectionName => "Npcs";

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public required string Name { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Basic attributes
    public int Level { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public Dictionary<string, int> BaseStats { get; set; } = new();
    public Dictionary<string, int> ModifiedStats { get; set; } = new();

    // Spawn Location
    public LocationData SpawnLocation { get; set; } = new();
    public LocationData CurrentLocation { get; set; } = new();
    public bool IsMoving { get; set; }
    public bool IsRotating { get; set; }

    [BsonRepresentation(BsonType.String)] public Guid WorldId { get; set; }

    // Tags for categorization (e.g., "friendly", "hostile", "merchant", "boss")
    public List<string> Tags { get; set; } = new();

    // Components stored as JSON strings
    public List<ComponentData> Components { get; set; } = new();

    // Skills (Skill ID -> SkillAvailability)
    public Dictionary<string, string> Skills { get; set; } = new(); // Skill ID -> SkillAvailability enum as string
    public Dictionary<string, DateTime> ActiveSkills { get; set; } = new(); // Skill ID -> Activation time

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
