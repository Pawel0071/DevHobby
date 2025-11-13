using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Models;

public class WorldStateDocument : IPersistenceModel
{
    public static string CollectionName => "Worlds";

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    public string WorldName { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public List<Guid> Characters { get; set; } = new();
    public List<Guid> Npcs { get; set; } = new();
    public List<Guid> MapObjects { get; set; } = new();
}
