using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Documents;

/// <summary>
///     MongoDB document representing world state.
///     Minimal version - stores only basic world information.
/// </summary>
public class WorldStateDocument : IMongoDocument
{
    public static string CollectionName => "Worlds";

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)] public Guid WorldId { get; set; }

    public string WorldName { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
