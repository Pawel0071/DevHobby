using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Documents;

/// <summary>
///     MongoDB document representing a player account.
///     Minimal version - stores only basic account information.
/// </summary>
public class PlayerDocument : IMongoDocument
{
    public static string CollectionName => "Players";

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public required string Username { get; set; }
    public string Email { get; set; } = string.Empty;

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }

    // State
    public bool IsOnline { get; set; }
    public bool IsBanned { get; set; }
    public DateTime? BannedUntil { get; set; }
}
