using System.Numerics;
using FluentAssertions;
using Moq;
using RPG.Domain.Entities;
using RPG.Domain.Enums;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers;

namespace RPG.UnitTest.Infrastructure.Mappers;

/// <summary>
///     Tests for CharacterModelMapper - mapping between Character entity and CharacterDocument
/// </summary>
public class CharacterModelMapperTests
{
    private readonly CharacterModelMapper _mapper;

    public CharacterModelMapperTests()
    {
        var logger = new Mock<ILogger<CharacterModelMapper>>();
        var locationLogger = new Mock<ILogger<LocationMapper>>();
        _mapper = new CharacterModelMapper(logger.Object, new LocationMapper(locationLogger.Object));
    }

    [Fact]
    public void ToDocument_MapsBasicProperties()
    {
        // Arrange
        var character = new Character(
            Guid.NewGuid(),
            CharacterClass.Warrior
        )
        {
            Id = Guid.NewGuid(),
            Name = "Geralt"
        };

        // Act
        var document = _mapper.ToPersistence(character);

        // Assert
        document.Should().NotBeNull();
        document.Id.Should().Be(character.Id);
        document.Name.Should().Be("Geralt");
        document.Class.Should().Be(CharacterClass.Warrior.ToString());
    }

    [Fact]
    public void ToDocument_MapsSessionAndPlayerIds()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        var character = new Character(sessionId, CharacterClass.Mage)
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            PlayerId = playerId
        };

        // Act
        var document = _mapper.ToPersistence(character);

        // Assert
        document.SessionId.Should().Be(sessionId);
        document.PlayerId.Should().Be(playerId);
    }

    [Fact]
    public void ToDomain_MapsBasicProperties()
    {
        // Arrange
        var document = new CharacterDocument
        {
            Id = Guid.NewGuid(),
            Name = "Geralt",
            Class = CharacterClass.Warrior.ToString(),
            SessionId = Guid.NewGuid(),
            PlayerId = Guid.NewGuid()
        };

        // Act
        var character = _mapper.ToDomain(document);

        // Assert
        character.Should().NotBeNull();
        character.Id.Should().Be(document.Id);
        character.Name.Should().Be("Geralt");
        character.Class.Should().Be(CharacterClass.Warrior);
    }

    [Fact]
    public void RoundTrip_PreservesBasicData()
    {
        // Arrange
        var originalCharacter = new Character(Guid.NewGuid(), CharacterClass.Assassin)
        {
            Id = Guid.NewGuid(),
            Name = "Geralt",
            PlayerId = Guid.NewGuid()
        };

        // Act - convert to document and back
        var document = _mapper.ToPersistence(originalCharacter);
        var roundTrippedCharacter = _mapper.ToDomain(document);

        // Assert - basic properties should match
        roundTrippedCharacter.Id.Should().Be(originalCharacter.Id);
        roundTrippedCharacter.Name.Should().Be(originalCharacter.Name);
        roundTrippedCharacter.Class.Should().Be(originalCharacter.Class);
        roundTrippedCharacter.SessionId.Should().Be(originalCharacter.SessionId);
        roundTrippedCharacter.PlayerId.Should().Be(originalCharacter.PlayerId);
    }

    [Fact]
    public void ToDocument_HandlesNullPlayerId()
    {
        // Arrange
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "Test"
            // PlayerId not set (default Guid.Empty)
        };

        // Act
        var document = _mapper.ToPersistence(character);

        // Assert
        document.Should().NotBeNull();
        // Should handle Guid.Empty gracefully
        document.PlayerId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ToDocument_WithEmptyInventories_ShouldMapInventorySlots()
    {
        // Arrange
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "EmptyInventoryTest"
        };

        // Act
        var document = _mapper.ToPersistence(character);

        // Assert
        document.Backpack.Should().NotBeNull();
        document.Bank.Should().NotBeNull();
        document.Equipment.Should().NotBeNull();
        document.Skills.Should().NotBeNull();
        document.ActiveSkills.Should().NotBeNull();
    }

    [Fact]
    public void ToDocument_MapsLocation()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "LocationHero"
        };

        var worldId = Guid.NewGuid();
        var location = Location.Create(new Vector3(5, 1, -3), worldId, "Map-1", "Zone-9");
        location.Rotation = 180f;
        character.SetCurrentLocation(location);
    character.SetMovementState(true);
    character.SetRotationState(true);

        var document = _mapper.ToPersistence(character);

        document.Location.X.Should().Be(5f);
        document.Location.Y.Should().Be(1f);
        document.Location.Z.Should().Be(-3f);
        document.Location.WorldId.Should().Be(worldId.ToString());
        document.Location.MapId.Should().Be("Map-1");
        document.Location.ZoneName.Should().Be("Zone-9");
        document.Location.Rotation.Should().Be(180f);
    document.IsMoving.Should().BeTrue();
    document.IsRotating.Should().BeTrue();
    }

    [Fact]
    public void ToDomain_MapsLocation()
    {
        var worldId = Guid.NewGuid();
        var document = new CharacterDocument
        {
            Id = Guid.NewGuid(),
            Name = "MapperHero",
            Class = CharacterClass.Mage.ToString(),
            SessionId = Guid.NewGuid(),
            PlayerId = Guid.NewGuid(),
            Location = new LocationData
            {
                X = -2f,
                Y = 0.5f,
                Z = 9f,
                WorldId = worldId.ToString(),
                MapId = "Map-77",
                ZoneName = "Dungeon",
                Rotation = 90f
            },
            IsMoving = true,
            IsRotating = false
        };

        var character = _mapper.ToDomain(document);

        character.CurrentLocation.Position.X.Should().Be(-2f);
        character.CurrentLocation.Position.Y.Should().Be(0.5f);
        character.CurrentLocation.Position.Z.Should().Be(9f);
        character.CurrentLocation.WorldId.Should().Be(worldId);
        character.CurrentLocation.MapId.Should().Be("Map-77");
        character.CurrentLocation.ZoneName.Should().Be("Dungeon");
        character.CurrentLocation.Rotation.Should().Be(90f);
        character.IsMoving.Should().BeTrue();
        character.IsRotating.Should().BeFalse();
    }
}
