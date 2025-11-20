using Grpc.Core;
using RPG.Application.Managers;
using RPG.Domain.Models;
using RPG.GameServer.Protos;
using Location = RPG.Domain.Models.Location;

namespace RPG.GameServer.Controllers;

public class SessionServiceImpl : SessionService.SessionServiceBase
{
    private readonly ISessionManager _sessionManager;
    private readonly Infrastructure.Interfaces.ILogger<SessionServiceImpl> _logger;

    public SessionServiceImpl(ISessionManager sessionManager, Infrastructure.Interfaces.ILogger<SessionServiceImpl> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public override async Task<SessionReply> CreateSession(CreateSessionRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CharacterId, out var characterId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "characterId must be a valid GUID."));
        if (!Guid.TryParse(request.PlayerId, out var playerId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "playerId must be a valid GUID."));

        var session = await _sessionManager.CreateAsync(
            playerId,
            characterId,
            ResolvePeerAddress(context),
            ResolveServerRegion(),
            ResolveClientVersion(context),
            context.CancellationToken);

        _logger.Info($"Created session {session.Id} for player {playerId} and character {characterId}.");
        return new SessionReply { Session = ToProto(session) };
    }

    public override async Task<SessionReply> GetSession(SessionIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.SessionId, out var sessionId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "sessionId must be a valid GUID."));

        var session = await _sessionManager.GetAsync(sessionId, context.CancellationToken)
                      ?? throw new RpcException(new Status(StatusCode.NotFound, "Session not found."));

        return new SessionReply { Session = ToProto(session) };
    }

    public override async Task<SessionReply> EndSession(EndSessionRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.SessionId, out var sessionId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "sessionId must be a valid GUID."));

        var session = await _sessionManager.EndAsync(sessionId, context.CancellationToken)
                      ?? throw new RpcException(new Status(StatusCode.NotFound, "Session not found."));

        _logger.Info($"Ended session {session.Id} for player {session.PlayerId}.");
        return new SessionReply { Session = ToProto(session) };
    }

    public override async Task<SessionReply> HeartbeatSession(SessionHeartbeatRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.SessionId, out var sessionId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "sessionId must be a valid GUID."));

        var location = request.Location is null ? null : FromProtoLocation(request.Location);
        var session = await _sessionManager.HeartbeatAsync(sessionId, location, context.CancellationToken)
                      ?? throw new RpcException(new Status(StatusCode.NotFound, "Session not found."));

        return new SessionReply { Session = ToProto(session) };
    }

    // Mapping helpers remain transport-only.
    private static Session ToProto(GameSession session)
    {
        var message = new Session
        {
            Id = session.Id.ToString(),
            CharacterId = session.CharacterId?.ToString() ?? string.Empty,
            PlayerId = session.PlayerId.ToString(),
            StartedAt = new DateTimeOffset(session.StartedAt).ToUnixTimeMilliseconds(),
            Active = session.IsActive,
            LastActivityAt = new DateTimeOffset(session.LastActivityAt).ToUnixTimeMilliseconds(),
            Status = session.Status.ToString()
        };

        if (session.CurrentLocation != null)
        {
            message.LastKnownLocation = ToProtoLocation(session.CurrentLocation);
        }

        return message;
    }

    private static Protos.Location ToProtoLocation(Location location)
    {
        return new Protos.Location
        {
            X = location.Position.X,
            Y = location.Position.Y,
            Z = location.Position.Z,
            WorldId = location.WorldId.ToString(),
            MapId = location.MapId ?? string.Empty,
            ZoneName = location.MapName ?? string.Empty,
            Rotation = location.Direction
        };
    }

    private static Location FromProtoLocation(Protos.Location location)
    {
        var hasWorldId = Guid.TryParse(location.WorldId, out var parsedWorldId);
        var worldId = hasWorldId ? parsedWorldId : Guid.Empty;
        var mapId = location.MapId ?? string.Empty;
        var zoneName = location.ZoneName ?? string.Empty;

        var entityLocation = Location.Create(
            (float)location.X,
            (float)location.Y,
            (float)location.Z,
            worldId,
            mapId,
            zoneName);

        entityLocation.WorldId = hasWorldId ? parsedWorldId : Guid.Empty;
        entityLocation.Direction = location.Rotation;
        return entityLocation;
    }

    private static string ResolvePeerAddress(ServerCallContext context)
    {
        var peer = context.Peer;
        return string.IsNullOrWhiteSpace(peer) ? "unknown" : peer;
    }

    private static string ResolveClientVersion(ServerCallContext context)
    {
        var header = context.RequestHeaders.FirstOrDefault(h =>
            string.Equals(h.Key, "x-client-version", StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(header?.Value) ? "desktop-client" : header.Value;
    }

    private static string ResolveServerRegion()
    {
        return Environment.GetEnvironmentVariable("RPG_SERVER_REGION") ?? "global";
    }
}
