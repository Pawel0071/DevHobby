namespace RPG.Infrastructure.Documents;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Topic { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public bool Sent { get; set; } = false;
    public int RetryCount { get; set; } = 0;
    public DateTime? LastRetryAt { get; set; }
}