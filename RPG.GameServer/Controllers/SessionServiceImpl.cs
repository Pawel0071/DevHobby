using System.Collections.Concurrent;
using System.Linq;
using Grpc.Core;
using RPG.Domain.Entities;
using RPG.Domain.Enums;
using RPG.GameServer.Protos;
using RPG.Infrastructure.Interfaces;

namespace RPG.GameServer.Controllers;

public class SessionServiceImpl : SessionService.SessionServiceBase
{
	private static readonly ConcurrentDictionary<Guid, GameSession> Sessions = new();
	private readonly IDocumentRepository _documentRepository;
	private readonly Infrastructure.Interfaces.ILogger<SessionServiceImpl> _logger;

	public SessionServiceImpl(IDocumentRepository documentRepository, Infrastructure.Interfaces.ILogger<SessionServiceImpl> logger)
	{
		_documentRepository = documentRepository;
		_logger = logger;
	}

	public override async Task<SessionReply> CreateSession(CreateSessionRequest request, ServerCallContext context)
	{
		if (!Guid.TryParse(request.CharacterId, out var characterId))
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "characterId must be a valid GUID."));
		}

		if (!Guid.TryParse(request.PlayerId, out var playerId))
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "playerId must be a valid GUID."));
		}

		var sessionId = Guid.NewGuid();
		var now = DateTime.UtcNow;
		await EndActiveSessionsForCharacterAsync(characterId, now, context.CancellationToken).ConfigureAwait(false);

		var session = GameSession.Create(
			playerId,
			ResolvePeerAddress(context),
			ResolveServerRegion(),
			ResolveClientVersion(context),
			sessionId);

		session.AttachCharacter(characterId);
		session.UpdateActivity(now);

		Sessions[sessionId] = session;

		await _documentRepository.UpsertAsync(session, context.CancellationToken).ConfigureAwait(false);

		_logger.Info($"Created session {sessionId} for player {playerId} and character {characterId}.");

		return new SessionReply { Session = ToProto(session) };
	}

	public override async Task<SessionReply> GetSession(SessionIdRequest request, ServerCallContext context)
	{
		if (!Guid.TryParse(request.SessionId, out var sessionId))
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "sessionId must be a valid GUID."));
		}

		var session = await GetOrLoadSessionAsync(sessionId, context.CancellationToken).ConfigureAwait(false);
		return new SessionReply { Session = ToProto(session) };
	}

	public override async Task<SessionReply> EndSession(EndSessionRequest request, ServerCallContext context)
	{
		if (!Guid.TryParse(request.SessionId, out var sessionId))
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "sessionId must be a valid GUID."));
		}

		var session = await GetOrLoadSessionAsync(sessionId, context.CancellationToken).ConfigureAwait(false);

		if (!session.IsActive)
		{
			return new SessionReply { Session = ToProto(session) };
		}

		var now = DateTime.UtcNow;
		session.MarkEnded(now);
		Sessions[sessionId] = session;

		await _documentRepository.UpsertAsync(session, context.CancellationToken).ConfigureAwait(false);

		_logger.Info($"Ended session {sessionId} for player {session.PlayerId}.");

		return new SessionReply { Session = ToProto(session) };
	}

	public override async Task<SessionReply> HeartbeatSession(SessionHeartbeatRequest request, ServerCallContext context)
	{
		if (!Guid.TryParse(request.SessionId, out var sessionId))
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "sessionId must be a valid GUID."));
		}

		var session = await GetOrLoadSessionAsync(sessionId, context.CancellationToken).ConfigureAwait(false);

		if (session.Status == GameSessionStatus.Ended)
		{
			throw new RpcException(new Status(StatusCode.FailedPrecondition, "Session already ended."));
		}

		var now = DateTime.UtcNow;
		var location = request.Location is null ? null : FromProtoLocation(request.Location);
		session.UpdateActivity(now, location);

		if (!session.IsActive)
		{
			session.Status = GameSessionStatus.Connected;
		}

		Sessions[sessionId] = session;
		await _documentRepository.UpsertAsync(session, context.CancellationToken).ConfigureAwait(false);

		return new SessionReply { Session = ToProto(session) };
	}

	private async Task<GameSession> GetOrLoadSessionAsync(Guid sessionId, CancellationToken cancellationToken)
	{
		if (Sessions.TryGetValue(sessionId, out var cached))
		{
			return cached;
		}

		var loaded = await _documentRepository.GetByIdAsync<GameSession>(sessionId, cancellationToken).ConfigureAwait(false);
		if (loaded == null)
		{
			throw new RpcException(new Status(StatusCode.NotFound, "Session not found."));
		}

		Sessions[sessionId] = loaded;
		return loaded;
	}

	private async Task EndActiveSessionsForCharacterAsync(Guid characterId, DateTime timestamp, CancellationToken cancellationToken)
	{
		foreach (var (sessionId, existingSession) in Sessions.ToArray())
		{
			if (existingSession.CharacterId != characterId || !existingSession.IsActive)
			{
				continue;
			}

			existingSession.MarkEnded(timestamp);
			Sessions[sessionId] = existingSession;
			await _documentRepository.UpsertAsync(existingSession, cancellationToken).ConfigureAwait(false);

			_logger.Info($"Ended previous session {sessionId} for character {characterId} before creating a new one.");
		}
	}

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

	private static Protos.Location ToProtoLocation(RPG.Domain.Entities.Location location)
	{
		return new Protos.Location
		{
			X = location.Position.X,
			Y = location.Position.Y,
			Z = location.Position.Z
		};
	}

	private static RPG.Domain.Entities.Location FromProtoLocation(Protos.Location location)
	{
		return RPG.Domain.Entities.Location.Create(
			(float)location.X,
			(float)location.Y,
			(float)location.Z,
			Guid.Empty);
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
