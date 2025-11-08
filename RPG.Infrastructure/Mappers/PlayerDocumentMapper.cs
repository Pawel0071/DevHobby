using RPG.Domain.Entities;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between Player domain entity and PlayerDocument
/// </summary>
public class PlayerDocumentMapper : IDocumentMapper<Player, PlayerDocument>
{
    private readonly ILogger<PlayerDocumentMapper> _logger;

    public PlayerDocumentMapper(ILogger<PlayerDocumentMapper> logger)
    {
        _logger = logger;
    }

    public PlayerDocument ToDocument(Player entity)
    {
        _logger.Debug($"Converting Player to PlayerDocument. Id={entity.Id}, Username={entity.Username}");
        return new PlayerDocument
        {
            Id = entity.Id,
            Username = entity.Username,
            Email = entity.Email,
            CreatedAt = entity.CreatedAt,
            LastLoginAt = entity.LastLoginAt,
            IsOnline = entity.IsOnline,
            IsBanned = entity.IsBanned,
            BannedUntil = entity.BannedUntil
        };
    }

    public Player ToDomain(PlayerDocument document)
    {
        _logger.Debug($"Converting PlayerDocument to Player. Id={document.Id}, Username={document.Username}");
        var player = Player.Create(document.Username, document.Email);

        // Update fields using reflection or setters since Create() sets defaults
        typeof(Player).GetProperty("Id")!.SetValue(player, document.Id);
        typeof(Player).GetProperty("CreatedAt")!.SetValue(player, document.CreatedAt);
        player.LastLoginAt = document.LastLoginAt;
        player.IsOnline = document.IsOnline;
        player.IsBanned = document.IsBanned;
        player.BannedUntil = document.BannedUntil;

        return player;
    }

    public Player ToEntity(PlayerDocument document) => ToDomain(document);
}
