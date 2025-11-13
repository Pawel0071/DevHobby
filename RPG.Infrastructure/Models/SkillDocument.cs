using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Models;

public class SkillDocument : IPersistenceModel
{
    public static string CollectionName => "Skills";

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string IconId { get; set; } = string.Empty;

    // Tags for categorization (e.g., "offensive", "defensive", "fire_damage", "instant_cast")
    public List<string> Tags { get; set; } = new();

    // Components stored as JSON strings (damage, healing, buffs, cooldowns, etc.)
    public List<ComponentData> Components { get; set; } = new();
}
