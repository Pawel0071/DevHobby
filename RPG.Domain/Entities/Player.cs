using RPG.Domain.Common;

namespace RPG.Domain.Entities;

/// <summary>
///     Domain entity representing a player account.
///     Pure data entity - logic handled by services.
/// </summary>
public class Player : IDomainModel
{
    private Player()
    {
        Email = string.Empty;
        Username = string.Empty;
    }

    public Guid Id { get; private set; }
    public string Username { get; set; }
    public string Email { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime LastLoginAt { get; set; }

    // State
    public bool IsOnline { get; set; }
    public bool IsBanned { get; set; }
    public DateTime? BannedUntil { get; set; }

    public static Player Create(string username, string email)
    {
        return new Player
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };
    }
}
