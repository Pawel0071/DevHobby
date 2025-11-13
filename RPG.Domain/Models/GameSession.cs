using RPG.Domain.Common;
using RPG.Domain.Enums;

namespace RPG.Domain.Models;

/// <summary>
///     Domain entity representing an active game session.
///     Tracks player connection, character selection, and session state.
///     Pure data entity - logic handled by services.
/// </summary>
public class GameSession : IDomainModel
{
    private GameSession()
    {
        IpAddress = string.Empty;
        ServerRegion = string.Empty;
        ClientVersion = string.Empty;
    }

    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid? CharacterId { get; set; }

    // Session State
    public GameSessionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime LastActivityAt { get; set; }

    // Connection Info
    public string IpAddress { get; set; }
    public string ServerRegion { get; set; }
    public string ClientVersion { get; set; }

    // Current State
    public Guid? CurrentWorldId { get; set; }
    public string? CurrentZoneId { get; set; }
    public Location? CurrentLocation { get; set; }

    // Session Metrics
    public long SessionDurationSeconds { get; set; }
    public int ActionsPerformed { get; set; }
    public int MonstersKilled { get; set; }
    public int QuestsCompleted { get; set; }
    public long GoldEarned { get; set; }
    public long ExperienceGained { get; set; }

    // Party/Group
    public Guid? PartyId { get; set; }
    public bool IsPartyLeader { get; set; }

    // Instance/Dungeon
    public Guid? CurrentInstanceId { get; set; }

    // Session Flags
    public bool IsAfk { get; set; }
    public DateTime? AfkSince { get; set; }
    public bool IsInCombat { get; set; }
    public DateTime? CombatStartedAt { get; set; }

    // Disconnection Handling
    public int DisconnectCount { get; set; }
    public DateTime? LastDisconnectAt { get; set; }

    public static GameSession Create(
        Guid playerId,
        string ipAddress,
        string serverRegion,
        string clientVersion,
        Guid? sessionId = null)
    {
        return new GameSession
        {
            Id = sessionId ?? Guid.NewGuid(),
            PlayerId = playerId,
            Status = GameSessionStatus.Connected,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            ServerRegion = serverRegion,
            ClientVersion = clientVersion
        };
    }

    public bool IsActive => Status is GameSessionStatus.Connected or GameSessionStatus.InGame;

    public void AttachCharacter(Guid characterId)
    {
        CharacterId = characterId;
    }

    public void UpdateActivity(DateTime timestamp, Location? location = null)
    {
        LastActivityAt = timestamp;
        if (location != null)
        {
            CurrentLocation = location;
        }

        SessionDurationSeconds = (long)Math.Max(0, (timestamp - StartedAt).TotalSeconds);
    }

    public void MarkEnded(DateTime timestamp)
    {
        Status = GameSessionStatus.Ended;
        EndedAt = timestamp;
        UpdateActivity(timestamp);
    }

    public void MarkDisconnected(DateTime timestamp)
    {
        Status = GameSessionStatus.Disconnected;
        UpdateActivity(timestamp);
    }
}
