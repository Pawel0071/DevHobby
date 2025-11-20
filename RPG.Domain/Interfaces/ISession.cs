namespace RPG.Domain.Interfaces;

public interface ISession
{
    public Guid PlayerId { get; init; }
    public Guid SessionId { get; init; }
    public bool IsOnline { get; set; }
    public DateTime LastUpdated { get; set; }
}
