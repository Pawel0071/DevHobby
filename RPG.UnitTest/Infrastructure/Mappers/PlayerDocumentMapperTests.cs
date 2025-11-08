using FluentAssertions;
using Moq;
using RPG.Domain.Entities;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers;

namespace RPG.UnitTest.Infrastructure.Mappers;

/// <summary>
///     Tests for PlayerDocumentMapper - Player to/from PlayerDocument conversion
/// </summary>
public class PlayerDocumentMapperTests
{
    private readonly PlayerDocumentMapper _mapper;

    public PlayerDocumentMapperTests()
    {
        var mockLogger = new Mock<ILogger<PlayerDocumentMapper>>();
        _mapper = new PlayerDocumentMapper(mockLogger.Object);
    }

    [Fact]
    public void ToDocument_ShouldMapAllPlayerProperties()
    {
        // Arrange
        var player = Player.Create("TestPlayer", "test@example.com");
        player.LastLoginAt = DateTime.UtcNow;
        player.IsOnline = true;
        player.IsBanned = false;
        player.BannedUntil = null;

        // Act
        var document = _mapper.ToDocument(player);

        // Assert
        document.Id.Should().Be(player.Id);
        document.Username.Should().Be("TestPlayer");
        document.Email.Should().Be("test@example.com");
        document.CreatedAt.Should().Be(player.CreatedAt);
        document.LastLoginAt.Should().Be(player.LastLoginAt);
        document.IsOnline.Should().BeTrue();
        document.IsBanned.Should().BeFalse();
        document.BannedUntil.Should().BeNull();
    }

    [Fact]
    public void ToDocument_WithBannedPlayer_ShouldMapBanInformation()
    {
        // Arrange
        var player = Player.Create("BannedPlayer", "banned@example.com");
        var banUntil = DateTime.UtcNow.AddDays(7);
        player.IsBanned = true;
        player.BannedUntil = banUntil;

        // Act
        var document = _mapper.ToDocument(player);

        // Assert
        document.IsBanned.Should().BeTrue();
        document.BannedUntil.Should().Be(banUntil);
    }

    [Fact]
    public void ToDocument_WithOfflinePlayer_ShouldMapCorrectly()
    {
        // Arrange
        var player = Player.Create("OfflinePlayer", "offline@example.com");
        player.IsOnline = false;
        player.LastLoginAt = DateTime.UtcNow.AddHours(-5);

        // Act
        var document = _mapper.ToDocument(player);

        // Assert
        document.IsOnline.Should().BeFalse();
        document.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(-5), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToEntity_ShouldMapAllPlayerProperties()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddMonths(-3);
        var lastLoginAt = DateTime.UtcNow.AddHours(-2);
        
        var document = new PlayerDocument
        {
            Id = playerId,
            Username = "TestUser",
            Email = "user@test.com",
            CreatedAt = createdAt,
            LastLoginAt = lastLoginAt,
            IsOnline = true,
            IsBanned = false,
            BannedUntil = null
        };

        // Act
        var player = _mapper.ToEntity(document);

        // Assert
        player.Id.Should().Be(playerId);
        player.Username.Should().Be("TestUser");
        player.Email.Should().Be("user@test.com");
        player.CreatedAt.Should().Be(createdAt);
        player.LastLoginAt.Should().Be(lastLoginAt);
        player.IsOnline.Should().BeTrue();
        player.IsBanned.Should().BeFalse();
        player.BannedUntil.Should().BeNull();
    }

    [Fact]
    public void ToEntity_WithBannedPlayer_ShouldMapBanInformation()
    {
        // Arrange
        var banUntil = DateTime.UtcNow.AddDays(14);
        var document = new PlayerDocument
        {
            Id = Guid.NewGuid(),
            Username = "BannedUser",
            Email = "banned@test.com",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsOnline = false,
            IsBanned = true,
            BannedUntil = banUntil
        };

        // Act
        var player = _mapper.ToEntity(document);

        // Assert
        player.IsBanned.Should().BeTrue();
        player.BannedUntil.Should().Be(banUntil);
    }

    [Fact]
    public void ToEntity_WithDefaultLastLogin_ShouldHandleCorrectly()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var document = new PlayerDocument
        {
            Id = Guid.NewGuid(),
            Username = "NewUser",
            Email = "new@test.com",
            CreatedAt = createdAt,
            LastLoginAt = createdAt, // Same as created for new user
            IsOnline = false,
            IsBanned = false,
            BannedUntil = null
        };

        // Act
        var player = _mapper.ToEntity(document);

        // Assert
        player.LastLoginAt.Should().Be(createdAt);
    }

    [Fact]
    public void RoundTrip_ShouldPreservePlayerData()
    {
        // Arrange
        var player = Player.Create("RoundTripUser", "roundtrip@test.com");
        player.LastLoginAt = DateTime.UtcNow.AddHours(-1);
        player.IsOnline = true;
        player.IsBanned = false;

        // Act
        var document = _mapper.ToDocument(player);
        var roundTrippedPlayer = _mapper.ToEntity(document);

        // Assert
        roundTrippedPlayer.Id.Should().Be(player.Id);
        roundTrippedPlayer.Username.Should().Be(player.Username);
        roundTrippedPlayer.Email.Should().Be(player.Email);
        roundTrippedPlayer.IsOnline.Should().Be(player.IsOnline);
        roundTrippedPlayer.IsBanned.Should().Be(player.IsBanned);
    }

    [Fact]
    public void RoundTrip_WithBannedPlayer_ShouldPreserveBanData()
    {
        // Arrange
        var player = Player.Create("BannedRoundTrip", "banned.roundtrip@test.com");
        var banUntil = DateTime.UtcNow.AddDays(30);
        player.IsBanned = true;
        player.BannedUntil = banUntil;

        // Act
        var document = _mapper.ToDocument(player);
        var roundTrippedPlayer = _mapper.ToEntity(document);

        // Assert
        roundTrippedPlayer.IsBanned.Should().BeTrue();
        roundTrippedPlayer.BannedUntil.Should().Be(banUntil);
    }
}
