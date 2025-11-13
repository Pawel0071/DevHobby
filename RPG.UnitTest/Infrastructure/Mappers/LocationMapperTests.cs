using System.Numerics;
using FluentAssertions;
using RPG.Infrastructure.Interfaces;
using Moq;
using RPG.Domain.Models;
using RPG.Infrastructure.Mappers;
using RPG.Infrastructure.Models;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Mappers;

/// <summary>
///     Tests for LocationMapper - Location to/from LocationData conversion
/// </summary>
public class LocationMapperTests
{
    private readonly LocationMapper _mapper;

    public LocationMapperTests()
    {
        var mockLogger = new Mock<ILogger<LocationMapper>>();
        _mapper = new LocationMapper(mockLogger.Object);
    }

    [Fact]
    public void ToDocument_ShouldMapAllLocationProperties()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var position = new Vector3(100.5f, 200.7f, 300.9f);
        var location = Location.Create(position, worldId, "TestMap", "TestZone");
        location.Rotation = 45.0f;

        // Act
        var document = _mapper.ToDocument(location);

        // Assert
        document.X.Should().Be(100.5f);
        document.Y.Should().Be(200.7f);
        document.Z.Should().Be(300.9f);
        document.WorldId.Should().Be(worldId.ToString());
        document.MapId.Should().Be("TestMap");
        document.ZoneName.Should().Be("TestZone");
        document.Rotation.Should().Be(45.0f);
    }

    [Fact]
    public void ToDocument_WithNullWorldId_ShouldMapCorrectly()
    {
        // Arrange
        var position = new Vector3(10, 20, 30);
        var location = Location.Create(position, Guid.Empty, "Map", "Zone");
        location.WorldId = null;

        // Act
        var document = _mapper.ToDocument(location);

        // Assert
        document.X.Should().Be(10);
        document.Y.Should().Be(20);
        document.Z.Should().Be(30);
        document.WorldId.Should().BeNull();
    }

    [Fact]
    public void ToDocument_WithZeroRotation_ShouldMapCorrectly()
    {
        // Arrange
        var location = Location.Create(new Vector3(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        location.Rotation = 0.0f;

        // Act
        var document = _mapper.ToDocument(location);

        // Assert
        document.Rotation.Should().Be(0.0f);
    }

    [Fact]
    public void ToDocument_WithNegativeCoordinates_ShouldMapCorrectly()
    {
        // Arrange
        var position = new Vector3(-50.5f, -100.7f, -150.9f);
        var location = Location.Create(position, Guid.NewGuid(), "Underground", "DarkZone");

        // Act
        var document = _mapper.ToDocument(location);

        // Assert
        document.X.Should().Be(-50.5f);
        document.Y.Should().Be(-100.7f);
        document.Z.Should().Be(-150.9f);
    }

    [Fact]
    public void ToEntity_ShouldMapAllLocationProperties()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var document = new LocationData
        {
            X = 250.5f,
            Y = 350.7f,
            Z = 450.9f,
            WorldId = worldId.ToString(),
            MapId = "CityMap",
            ZoneName = "Market",
            Rotation = 90.0f
        };

        // Act
        var location = _mapper.ToEntity(document);

        // Assert
        location.Position.X.Should().Be(250.5f);
        location.Position.Y.Should().Be(350.7f);
        location.Position.Z.Should().Be(450.9f);
        location.WorldId.Should().Be(worldId);
        location.MapId.Should().Be("CityMap");
        location.ZoneName.Should().Be("Market");
        location.Rotation.Should().Be(90.0f);
    }

    [Fact]
    public void ToEntity_WithNullWorldId_ShouldMapToNull()
    {
        // Arrange
        var document = new LocationData
        {
            X = 10,
            Y = 20,
            Z = 30,
            WorldId = null,
            MapId = "Map",
            ZoneName = "Zone",
            Rotation = 0
        };

        // Act
        var location = _mapper.ToEntity(document);

        // Assert
        location.WorldId.Should().BeNull();
    }

    [Fact]
    public void ToEntity_WithEmptyWorldId_ShouldMapToNull()
    {
        // Arrange
        var document = new LocationData
        {
            X = 10,
            Y = 20,
            Z = 30,
            WorldId = "",
            MapId = "Map",
            ZoneName = "Zone",
            Rotation = 0
        };

        // Act
        var location = _mapper.ToEntity(document);

        // Assert
        location.WorldId.Should().BeNull();
    }

    [Fact]
    public void ToEntity_WithZeroCoordinates_ShouldMapCorrectly()
    {
        // Arrange
        var document = new LocationData
        {
            X = 0,
            Y = 0,
            Z = 0,
            WorldId = Guid.NewGuid().ToString(),
            MapId = "StartMap",
            ZoneName = "Spawn",
            Rotation = 0
        };

        // Act
        var location = _mapper.ToEntity(document);

        // Assert
        location.Position.X.Should().Be(0);
        location.Position.Y.Should().Be(0);
        location.Position.Z.Should().Be(0);
    }

    [Fact]
    public void ToEntity_WithNegativeRotation_ShouldMapCorrectly()
    {
        // Arrange
        var document = new LocationData
        {
            X = 100,
            Y = 200,
            Z = 300,
            WorldId = Guid.NewGuid().ToString(),
            MapId = "Map",
            ZoneName = "Zone",
            Rotation = -45.0f
        };

        // Act
        var location = _mapper.ToEntity(document);

        // Assert
        location.Rotation.Should().Be(-45.0f);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveLocationData()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var position = new Vector3(123.45f, 234.56f, 345.67f);
        var location = Location.Create(position, worldId, "TestMap", "TestZone");
        location.Rotation = 180.0f;

        // Act
        var document = _mapper.ToDocument(location);
        var roundTrippedLocation = _mapper.ToEntity(document);

        // Assert
        roundTrippedLocation.Position.X.Should().Be(location.Position.X);
        roundTrippedLocation.Position.Y.Should().Be(location.Position.Y);
        roundTrippedLocation.Position.Z.Should().Be(location.Position.Z);
        roundTrippedLocation.WorldId.Should().Be(location.WorldId);
        roundTrippedLocation.MapId.Should().Be(location.MapId);
        roundTrippedLocation.ZoneName.Should().Be(location.ZoneName);
        roundTrippedLocation.Rotation.Should().Be(location.Rotation);
    }

    [Fact]
    public void RoundTrip_WithNullWorldId_ShouldPreserveNull()
    {
        // Arrange
        var location = Location.Create(new Vector3(50, 60, 70), Guid.Empty, "Map", "Zone");
        location.WorldId = null;

        // Act
        var document = _mapper.ToDocument(location);
        var roundTrippedLocation = _mapper.ToEntity(document);

        // Assert
        roundTrippedLocation.WorldId.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_WithExtremeCoordinates_ShouldPreserveValues()
    {
        // Arrange
        var position = new Vector3(float.MaxValue / 2, float.MinValue / 2, 0);
        var location = Location.Create(position, Guid.NewGuid(), "ExtremeMap", "ExtremeZone");

        // Act
        var document = _mapper.ToDocument(location);
        var roundTrippedLocation = _mapper.ToEntity(document);

        // Assert
        roundTrippedLocation.Position.X.Should().BeApproximately(location.Position.X, 0.01f);
        roundTrippedLocation.Position.Y.Should().BeApproximately(location.Position.Y, 0.01f);
        roundTrippedLocation.Position.Z.Should().Be(location.Position.Z);
    }
}
