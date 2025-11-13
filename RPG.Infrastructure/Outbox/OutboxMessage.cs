using RPG.Infrastructure.Models;

namespace RPG.Infrastructure.Outbox;

public class OutboxMessage : IPersistenceModel
{
    public static string CollectionName => "outbox";

    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Topic { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public int RetryCount { get; set; } = 0;
    public DateTime? LastRetryAt { get; set; }
}
