using RPG.Domain.Enums;

namespace RPG.Domain.Entities;

/// <summary>
///     Domain entity representing an active game session.
///     Tracks player connection, character selection, and session state.
///     Pure data entity - logic handled by services.
/// </summary>
public class GameSession
{
    private GameSession()
    {
        IpAddress = string.Empty;
        ServerRegion = string.Empty;
        ClientVersion = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid PlayerId { get; private set; }
    public Guid? CharacterId { get; set; }

    // Session State
    public GameSessionStatus Status { get; set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; set; }
    public DateTime LastActivityAt { get; set; }

    // Connection Info
    public string IpAddress { get; private set; }
    public string ServerRegion { get; private set; }
    public string ClientVersion { get; private set; }

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
        string clientVersion)
    {
        return new GameSession
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Status = GameSessionStatus.Connected,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            ServerRegion = serverRegion,
            ClientVersion = clientVersion
        };
    }
}
