using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Managers;

public interface ISessionManager
{
    Task<GameSession> CreateAsync(Guid playerId, Guid characterId, string ipAddress, string serverRegion, string clientVersion, CancellationToken ct);
    Task<GameSession?> GetAsync(Guid sessionId, CancellationToken ct);
    Task<GameSession?> HeartbeatAsync(Guid sessionId, Location? location, CancellationToken ct);
    Task<GameSession?> EndAsync(Guid sessionId, CancellationToken ct);
}

public sealed class SessionManager : ISessionManager
{
    private readonly IModelRepository _repo;

    public SessionManager(IModelRepository repo)
    {
        _repo = repo;
    }

    public async Task<GameSession> CreateAsync(Guid playerId, Guid characterId, string ipAddress, string serverRegion, string clientVersion, CancellationToken ct)
    {
        var session = GameSession.Create(playerId, ipAddress, serverRegion, clientVersion);
        session.AttachCharacter(characterId);
        await _repo.UpsertAsync(session, ct);
        return session;
    }

    public Task<GameSession?> GetAsync(Guid sessionId, CancellationToken ct)
    {
        return _repo.GetByIdAsync<GameSession>(sessionId, ct);
    }

    public async Task<GameSession?> HeartbeatAsync(Guid sessionId, Location? location, CancellationToken ct)
    {
        var session = await _repo.GetByIdAsync<GameSession>(sessionId, ct);
        if (session == null) return null;

        session.UpdateActivity(DateTime.UtcNow, location);
        if (location != null)
        {
            session.CurrentWorldId = location.WorldId;
            session.CurrentZoneId = location.ZoneName;
        }

        await _repo.UpsertAsync(session, ct);
        return session;
    }

    public async Task<GameSession?> EndAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await _repo.GetByIdAsync<GameSession>(sessionId, ct);
        if (session == null) return null;

        session.MarkEnded(DateTime.UtcNow);
        await _repo.UpsertAsync(session, ct);
        return session;
    }
}

