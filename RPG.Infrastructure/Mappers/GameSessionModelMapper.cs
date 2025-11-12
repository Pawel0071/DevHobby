using RPG.Domain.Entities;
using RPG.Domain.Enums;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between GameSession domain entity and GameSessionDocument.
/// </summary>
public sealed class GameSessionModelMapper : IModelMapper<GameSession, GameSessionDocument>
{
    private readonly LocationMapper _locationMapper;
    private readonly ILogger<GameSessionModelMapper> _logger;

    public GameSessionModelMapper(LocationMapper locationMapper, ILogger<GameSessionModelMapper> logger)
    {
        _locationMapper = locationMapper;
        _logger = logger;
    }

    public GameSessionDocument ToPersistence(GameSession entity)
    {
        _logger.Debug($"Converting GameSession {entity.Id} to document representation.");

        return new GameSessionDocument
        {
            Id = entity.Id,
            PlayerId = entity.PlayerId,
            CharacterId = entity.CharacterId,
            Status = entity.Status.ToString(),
            StartedAt = entity.StartedAt,
            EndedAt = entity.EndedAt,
            LastActivityAt = entity.LastActivityAt,
            IpAddress = entity.IpAddress,
            ServerRegion = entity.ServerRegion,
            ClientVersion = entity.ClientVersion,
            CurrentWorldId = entity.CurrentWorldId,
            CurrentZoneId = entity.CurrentZoneId,
            CurrentLocation = entity.CurrentLocation != null ? _locationMapper.ToDocument(entity.CurrentLocation) : null,
            SessionDurationSeconds = entity.SessionDurationSeconds,
            ActionsPerformed = entity.ActionsPerformed,
            MonstersKilled = entity.MonstersKilled,
            QuestsCompleted = entity.QuestsCompleted,
            GoldEarned = entity.GoldEarned,
            ExperienceGained = entity.ExperienceGained,
            PartyId = entity.PartyId,
            IsPartyLeader = entity.IsPartyLeader,
            CurrentInstanceId = entity.CurrentInstanceId,
            IsAfk = entity.IsAfk,
            AfkSince = entity.AfkSince,
            IsInCombat = entity.IsInCombat,
            CombatStartedAt = entity.CombatStartedAt,
            DisconnectCount = entity.DisconnectCount,
            LastDisconnectAt = entity.LastDisconnectAt
        };
    }

    public GameSession ToDomain(GameSessionDocument document)
    {
        _logger.Debug($"Converting GameSessionDocument {document.Id} to domain entity.");

        var session = GameSession.Create(
            document.PlayerId,
            document.IpAddress,
            document.ServerRegion,
            document.ClientVersion,
            document.Id);

        if (Enum.TryParse<GameSessionStatus>(document.Status, true, out var status))
        {
            session.Status = status;
        }
        else
        {
            _logger.Warn($"Unknown GameSessionStatus '{document.Status}' for session {document.Id}. Defaulting to Connected.");
            session.Status = GameSessionStatus.Connected;
        }

        session.StartedAt = document.StartedAt;
        session.EndedAt = document.EndedAt;
        session.LastActivityAt = document.LastActivityAt;
        session.CharacterId = document.CharacterId;
        session.CurrentWorldId = document.CurrentWorldId;
        session.CurrentZoneId = document.CurrentZoneId;
        session.CurrentLocation = document.CurrentLocation != null
            ? _locationMapper.ToEntity(document.CurrentLocation)
            : null;

        session.SessionDurationSeconds = document.SessionDurationSeconds;
        session.ActionsPerformed = document.ActionsPerformed;
        session.MonstersKilled = document.MonstersKilled;
        session.QuestsCompleted = document.QuestsCompleted;
        session.GoldEarned = document.GoldEarned;
        session.ExperienceGained = document.ExperienceGained;
        session.PartyId = document.PartyId;
        session.IsPartyLeader = document.IsPartyLeader;
        session.CurrentInstanceId = document.CurrentInstanceId;
        session.IsAfk = document.IsAfk;
        session.AfkSince = document.AfkSince;
        session.IsInCombat = document.IsInCombat;
        session.CombatStartedAt = document.CombatStartedAt;
        session.DisconnectCount = document.DisconnectCount;
        session.LastDisconnectAt = document.LastDisconnectAt;

        return session;
    }
}
