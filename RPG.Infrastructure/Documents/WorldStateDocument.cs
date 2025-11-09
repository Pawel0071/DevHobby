using System;
using System.Collections.Generic;
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

    public List<WorldCharacterStateDocument> Characters { get; set; } = new();
    public List<WorldNpcStateDocument> Npcs { get; set; } = new();
    public List<WorldMapObjectStateDocument> MapObjects { get; set; } = new();
}

public class WorldCharacterStateDocument
{
    [BsonRepresentation(BsonType.String)]
    public Guid CharacterId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid SessionId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public WorldLocationDocument Location { get; set; } = new();
    public bool IsOnline { get; set; }
    public bool IsInCombat { get; set; }
    public DateTime LastUpdated { get; set; }
    public HashSet<string> StatusEffects { get; set; } = new();
}

public class WorldNpcStateDocument
{
    [BsonRepresentation(BsonType.String)]
    public Guid NpcId { get; set; }

    public string Name { get; set; } = string.Empty;
    public WorldLocationDocument Location { get; set; } = new();
    public bool IsAlive { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime? RespawnAt { get; set; }
    public HashSet<string> Tags { get; set; } = new();
}

public class WorldMapObjectStateDocument
{
    [BsonRepresentation(BsonType.String)]
    public Guid MapObjectId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public WorldLocationDocument Location { get; set; } = new();
    public bool IsActive { get; set; }
    public HashSet<string> Tags { get; set; } = new();
    public Dictionary<string, string> State { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class WorldLocationDocument
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid? WorldId { get; set; }

    public string MapId { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public float Rotation { get; set; }
}
