using System.Text;
using System.Text.Json;
using Grpc.Core;
using RabbitMQ.Client;
using RPG.GameServer.Protos;
using StackExchange.Redis;

namespace RPG.GameServer.Controlers;

public class SessionServiceImpl : SessionService.SessionServiceBase
{
    private readonly IDatabase _redis;
    private readonly IModel _rabbitChannel;

    public SessionServiceImpl(IConnectionMultiplexer redis, IModel rabbitChannel)
    {
        _redis = redis.GetDatabase();
        _rabbitChannel = rabbitChannel;
    }

    public override async Task<SessionReply> CreateSession(CreateSessionRequest request, ServerCallContext context)
    {
        var session = new Session
        {
            Id = Guid.NewGuid().ToString(),
            CharacterId = request.CharacterId,
            StartedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            LastKnownLocation = new Location { X = 0, Y = 0, Z = 0 },
            Active = true
        };

        var json = JsonSerializer.Serialize(session);
        await _redis.StringSetAsync($"session:{session.Id}", json);

        PublishToRabbit("session.created", json);

        return new SessionReply { Session = session };
    }

    public override async Task<SessionReply> GetSession(SessionIdRequest request, ServerCallContext context)
    {
        var json = await _redis.StringGetAsync($"session:{request.SessionId}");
        if (json.IsNullOrEmpty)
            throw new RpcException(new Status(StatusCode.NotFound, "Session not found"));

        var session = JsonSerializer.Deserialize<Session>(json);
        return new SessionReply { Session = session };
    }

    public override async Task<SessionReply> EndSession(EndSessionRequest request, ServerCallContext context)
    {
        var json = await _redis.StringGetAsync($"session:{request.SessionId}");
        if (json.IsNullOrEmpty)
            throw new RpcException(new Status(StatusCode.NotFound, "Session not found"));

        var session = JsonSerializer.Deserialize<Session>(json);
        session.Active = false;

        var updatedJson = JsonSerializer.Serialize(session);
        await _redis.StringSetAsync($"session:{session.Id}", updatedJson);

        PublishToRabbit("session.ended", updatedJson);

        return new SessionReply { Session = session };
    }

    private void PublishToRabbit(string routingKey, string payload)
    {
        var body = Encoding.UTF8.GetBytes(payload);
        _rabbitChannel.BasicPublish(
            exchange: "rpg_exchange",
            routingKey: routingKey,
            basicProperties: null,
            body: body
        );
    }
}