using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Models;

public class QuestDocument : IPersistenceModel
{
    public static string CollectionName => "Quests";

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public string QuestGiverName { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)] public Guid? QuestGiverId { get; set; }

    // Location
    public LocationData StartLocation { get; set; } = new();
    public LocationData? TurnInLocation { get; set; }

    // Tags for categorization (e.g., "main", "side", "daily", "hard")
    public List<string> Tags { get; set; } = new();

    // Components stored as JSON strings (objectives, requirements, rewards)
    public List<ComponentData> Components { get; set; } = new();
}
