using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Documents;

/// <summary>
///     MongoDB document representing a player game session.
///     Persists session lifecycle, activity, and connection metadata.
/// </summary>
public class GameSessionDocument : IPersistenceModel
{
    public static string CollectionName => "GameSessions";

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid PlayerId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid? CharacterId { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime LastActivityAt { get; set; }

    public string IpAddress { get; set; } = string.Empty;
    public string ServerRegion { get; set; } = string.Empty;
    public string ClientVersion { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public Guid? CurrentWorldId { get; set; }
    public string? CurrentZoneId { get; set; }
    public LocationData? CurrentLocation { get; set; }

    public long SessionDurationSeconds { get; set; }
    public int ActionsPerformed { get; set; }
    public int MonstersKilled { get; set; }
    public int QuestsCompleted { get; set; }
    public long GoldEarned { get; set; }
    public long ExperienceGained { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid? PartyId { get; set; }
    public bool IsPartyLeader { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid? CurrentInstanceId { get; set; }

    public bool IsAfk { get; set; }
    public DateTime? AfkSince { get; set; }
    public bool IsInCombat { get; set; }
    public DateTime? CombatStartedAt { get; set; }

    public int DisconnectCount { get; set; }
    public DateTime? LastDisconnectAt { get; set; }
}
