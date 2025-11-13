using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Models;

/// <summary>
///     MongoDB document representing a player character.
///     Stores character state, equipment, inventory, skills, and progress.
/// </summary>
public class CharacterDocument : IPersistenceModel
{
    public static string CollectionName => "Characters";

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public required string Name { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid PlayerId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid SessionId { get; set; }

    public string Class { get; set; } = string.Empty; // CharacterClass enum as string

    // Level & Experience
    public int Level { get; set; }
    public long Experience { get; set; }
    public long ExperienceToNextLevel { get; set; }

    // Health & Resource
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentResource { get; set; } // Mana, Rage, Energy, etc.
    public int MaxResource { get; set; }

    // Stats (StatsProperty enum as string -> value)
    public Dictionary<string, int> BaseStats { get; set; } = new();
    public Dictionary<string, int> ModifiedStats { get; set; } = new();

    // Location
    public LocationData Location { get; set; } = new();
    public bool IsMoving { get; set; }
    public bool IsRotating { get; set; }

    // Equipment (EquipmentSlot enum as string -> Item ID)
    public Dictionary<string, Guid> Equipment { get; set; } = new();

    // Inventory
    public List<InventorySlotDocument> Backpack { get; set; } = new();
    public List<InventorySlotDocument> Bank { get; set; } = new();

    // Skills (Skill ID -> SkillAvailability)
    public Dictionary<string, string> Skills { get; set; } = new(); // Skill ID -> SkillAvailability enum as string
    public Dictionary<string, DateTime> ActiveSkills { get; set; } = new(); // Skill ID -> Activation time

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}


