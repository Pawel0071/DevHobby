// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Infrastructure/Outbox/RedisOutboxQueueWriter.cs
using System.Text.Json;
using RPG.Infrastructure.Interfaces;
using StackExchange.Redis;

namespace RPG.Infrastructure.Outbox;

public class RedisOutboxQueueWriter : IOutboxQueueWriter
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisOutboxQueueWriter> _logger;
    private const string PendingListKey = "outbox:pending";

    public RedisOutboxQueueWriter(IDatabase db, ILogger<RedisOutboxQueueWriter> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnqueueAsync(string topic, object payload, CancellationToken ct = default)
    {
        var msg = new OutboxMessage
        {
            Topic = topic,
            Payload = payload is string s ? s : JsonSerializer.Serialize(payload)
        };
        var serialized = JsonSerializer.Serialize(msg);
        await _db.ListLeftPushAsync(PendingListKey, serialized);
        _logger.Debug($"Enqueued outbox message topic={topic}");
    }
}

