using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Moq;
using RPG.Domain.Entities;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.MapObjects.MapObjectComponents;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Mappers;
using Xunit;
using RPG.Infrastructure.Interfaces;
using RPG.Domain.Entities.Items;

namespace RPG.UnitTest.Infrastructure.Mappers;

/// <summary>
///     Tests for MapObjectModelMapper - MapObject to/from MapObjectDocument conversion with all component types
/// </summary>
public class MapObjectModelMapperTests
{
    private readonly MapObjectModelMapper _mapper;
    private readonly LocationMapper _locationMapper;

    public MapObjectModelMapperTests()
    {
        var mockMapperLogger = new Mock<ILogger<MapObjectModelMapper>>();
        var mockLocationMapperLogger = new Mock<ILogger<LocationMapper>>();
        var mockItemMapper = new Mock<IModelMapper<Item, ItemDocument>>();
        _locationMapper = new LocationMapper(mockLocationMapperLogger.Object);
        _mapper = new MapObjectModelMapper(mockMapperLogger.Object, _locationMapper, mockItemMapper.Object);
    }

    [Fact]
    public void ToDocument_ShouldMapBasicMapObjectProperties()
    {
        // Arrange
        var location = Location.Create(new(100, 200, 300), Guid.NewGuid(), "TestMap", "TestZone");
        var worldId = Guid.NewGuid();
        var zoneId = "zone_" + Guid.NewGuid().ToString().Substring(0, 8);
        var mapObject = MapObject.Create("treasure_chest_01", location, worldId, zoneId);

        mapObject.DisplayName = "Ancient Treasure Chest";
        mapObject.Description = "A dusty old chest";
        mapObject.RotationYaw = 45.0f;
        mapObject.IsActive = true;
        mapObject.Tags = new HashSet<string> { "loot", "interactive" };
    var lastUpdated = DateTime.UtcNow.AddMinutes(-5);
    mapObject.LastUpdated = lastUpdated;
    mapObject.State["lockState"] = "closed";

        // Act
        var document = _mapper.ToPersistence(mapObject);

        // Assert
        document.Id.Should().Be(mapObject.Id);
        document.Name.Should().Be("treasure_chest_01");
        document.DisplayName.Should().Be("Ancient Treasure Chest");
        document.Description.Should().Be("A dusty old chest");
        document.RotationYaw.Should().Be(45.0f);
        document.WorldId.Should().Be(worldId);
        document.ZoneId.Should().Be(zoneId);
        document.IsActive.Should().BeTrue();
        document.Tags.Should().Contain("loot");
    document.State.Should().ContainKey("lockState");
    document.State["lockState"].Should().Be("closed");
    document.LastUpdated.Should().Be(lastUpdated);
    }

    [Fact]
    public void ToDocument_WithLockableComponent_ShouldSerializeComponent()
    {
        // Arrange
        var location = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var mapObject = MapObject.Create("locked_door", location, Guid.NewGuid(), "zone_" + Guid.NewGuid().ToString().Substring(0, 8));

        var lockableComponent = new LockableComponent
        {
            IsLocked = true,
            RequiredKeyItemId = "golden_key",
            LockpickDifficulty = 50,
            CanBeLockpicked = true
        };
        mapObject.Components.Add(lockableComponent);

        // Act
        var document = _mapper.ToPersistence(mapObject);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(LockableComponent));
        document.Components[0].Data.Should().Contain("true");
        document.Components[0].Data.Should().Contain("golden_key");
        document.Components[0].Data.Should().Contain("50");
    }

    [Fact]
    public void ToDocument_WithDoorComponent_ShouldSerializeComponent()
    {
        // Arrange
        var location = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var mapObject = MapObject.Create("door", location, Guid.NewGuid(), "zone_" + Guid.NewGuid().ToString().Substring(0, 8));

        var linkedDoorId = Guid.NewGuid();
        var doorComponent = new DoorComponent
        {
            IsOpen = false,
            LinkedDoorId = linkedDoorId,
            OpenAnimation = "door_open",
            CloseAnimation = "door_close",
            OpenAngle = 90.0f,
            AutoClose = true,
            AutoCloseDelaySeconds = 5
        };
        mapObject.Components.Add(doorComponent);

        // Act
        var document = _mapper.ToPersistence(mapObject);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(DoorComponent));
        document.Components[0].Data.Should().Contain("false");
        document.Components[0].Data.Should().Contain("door_open");
        document.Components[0].Data.Should().Contain("90");
    }

    [Fact]
    public void ToDocument_WithPortalComponent_ShouldSerializeComponent()
    {
        // Arrange
        var location = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var mapObject = MapObject.Create("portal", location, Guid.NewGuid(), "zone_" + Guid.NewGuid().ToString().Substring(0, 8));

        var destinationWorldId = Guid.NewGuid();
        var destinationLocation = Location.Create(new(500, 600, 700), destinationWorldId, "DestMap", "DestZone");
        var portalComponent = new PortalComponent
        {
            DestinationWorldId = destinationWorldId,
            DestinationZoneId = "zone_123",
            DestinationLocation = destinationLocation,
            RequiresActivation = true,
            IsActivated = false,
            MinimumLevel = 20,
            RequiredQuestIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };
        mapObject.Components.Add(portalComponent);

        // Act
        var document = _mapper.ToPersistence(mapObject);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(PortalComponent));
        document.Components[0].Data.Should().Contain("zone_123");
        document.Components[0].Data.Should().Contain("20");
    }

    [Fact]
    public void ToDocument_WithInteractionComponent_ShouldSerializeComponent()
    {
        // Arrange
        var location = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var mapObject = MapObject.Create("lever", location, Guid.NewGuid(), "zone_" + Guid.NewGuid().ToString().Substring(0, 8));

        var interactionComponent = new InteractionComponent
        {
            IsInteractable = true,
            InteractionRadius = 5.0f,
            InteractionPrompt = "Press E to pull lever",
            InteractionDurationMs = 2000,
            RequiresLineOfSight = true,
            MaxSimultaneousUsers = 1,
            CooldownSeconds = 30
        };
        mapObject.Components.Add(interactionComponent);

        // Act
        var document = _mapper.ToPersistence(mapObject);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(InteractionComponent));
        document.Components[0].Data.Should().Contain("Press E to pull lever");
        document.Components[0].Data.Should().Contain("5");
        document.Components[0].Data.Should().Contain("2000");
    }

    [Fact]
    public void ToDocument_WithMultipleComponents_ShouldSerializeAll()
    {
        // Arrange
        var location = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var mapObject = MapObject.Create("complex_object", location, Guid.NewGuid(), "zone_" + Guid.NewGuid().ToString().Substring(0, 8));

        mapObject.Components.Add(new LockableComponent { IsLocked = true, LockpickDifficulty = 25 });
        mapObject.Components.Add(new DoorComponent { IsOpen = false, AutoClose = true });
        mapObject.Components.Add(new InteractionComponent { InteractionPrompt = "Examine" });

        // Act
        var document = _mapper.ToPersistence(mapObject);

        // Assert
        document.Components.Should().HaveCount(3);
        document.Components.Should().Contain(c => c.Type == nameof(LockableComponent));
        document.Components.Should().Contain(c => c.Type == nameof(DoorComponent));
        document.Components.Should().Contain(c => c.Type == nameof(InteractionComponent));
    }

    [Fact]
    public void ToEntity_ShouldMapBasicMapObjectProperties()
    {
        // Arrange
        var objectId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var zoneId = "zone_" + Guid.NewGuid().ToString().Substring(0, 8);
        var document = new MapObjectDocument
        {
            Id = objectId,
            Name = "statue_01",
            DisplayName = "Ancient Statue",
            Description = "A weathered statue",
            Location = new LocationData { X = 10, Y = 20, Z = 30, WorldId = Guid.NewGuid().ToString(), MapId = "Map1", ZoneName = "Zone1" },
            RotationYaw = 90.0f,
            WorldId = worldId,
            ZoneId = zoneId,
            IsActive = true,
            Tags = new List<string> { "decoration", "ancient" },
            Components = new List<ComponentData>(),
            State = new Dictionary<string, string> { ["lockState"] = "open" },
            LastUpdated = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var mapObject = _mapper.ToEntity(document);

        // Assert
        mapObject.Id.Should().Be(objectId, "ID should be preserved from document");
        mapObject.Name.Should().Be("statue_01");
        mapObject.DisplayName.Should().Be("Ancient Statue");
        mapObject.Description.Should().Be("A weathered statue");
        mapObject.RotationYaw.Should().Be(90.0f);
        mapObject.WorldId.Should().Be(worldId);
        mapObject.ZoneId.Should().Be(zoneId);
        mapObject.IsActive.Should().BeTrue();
        mapObject.Tags.Should().Contain("decoration");
    mapObject.State.Should().ContainKey("lockState");
    mapObject.State["lockState"].Should().Be("open");
    mapObject.LastUpdated.Should().Be(document.LastUpdated);
    }

    [Fact]
    public void ToEntity_WithLockableComponent_ShouldDeserializeComponent()
    {
        // Arrange
        var lockableComponent = new LockableComponent
        {
            IsLocked = true,
            RequiredKeyItemId = "silver_key",
            LockpickDifficulty = 75,
            CanBeLockpicked = false
        };
        var componentData = new ComponentData
        {
            Type = nameof(LockableComponent),
            Data = JsonSerializer.Serialize(lockableComponent)
        };

        var document = new MapObjectDocument
        {
            Id = Guid.NewGuid(),
            Name = "locked_chest",
            Location = new LocationData { X = 0, Y = 0, Z = 0, WorldId = Guid.NewGuid().ToString(), MapId = "Map", ZoneName = "Zone" },
            WorldId = Guid.NewGuid(),
            ZoneId = "zone_" + Guid.NewGuid().ToString().Substring(0, 8),
            Tags = new List<string>(),
            Components = new List<ComponentData> { componentData }
        };

        // Act
        var mapObject = _mapper.ToEntity(document);

        // Assert
        mapObject.Components.Should().HaveCount(1);
        var component = mapObject.Components[0] as LockableComponent;
        component.Should().NotBeNull();
        component!.IsLocked.Should().BeTrue();
        component.RequiredKeyItemId.Should().Be("silver_key");
        component.LockpickDifficulty.Should().Be(75);
        component.CanBeLockpicked.Should().BeFalse();
    }

    [Fact]
    public void ToEntity_WithDoorComponent_ShouldDeserializeComponent()
    {
        // Arrange
        var linkedDoorId = Guid.NewGuid();
        var doorComponent = new DoorComponent
        {
            IsOpen = true,
            LinkedDoorId = linkedDoorId,
            OpenAnimation = "swing_open",
            OpenAngle = 120.0f,
            AutoClose = false
        };
        var componentData = new ComponentData
        {
            Type = nameof(DoorComponent),
            Data = JsonSerializer.Serialize(doorComponent)
        };

        var document = new MapObjectDocument
        {
            Id = Guid.NewGuid(),
            Name = "entrance_door",
            Location = new LocationData { X = 0, Y = 0, Z = 0, WorldId = Guid.NewGuid().ToString(), MapId = "Map", ZoneName = "Zone" },
            WorldId = Guid.NewGuid(),
            ZoneId = "zone_" + Guid.NewGuid().ToString().Substring(0, 8),
            Tags = new List<string>(),
            Components = new List<ComponentData> { componentData }
        };

        // Act
        var mapObject = _mapper.ToEntity(document);

        // Assert
        mapObject.Components.Should().HaveCount(1);
        var component = mapObject.Components[0] as DoorComponent;
        component.Should().NotBeNull();
        component!.IsOpen.Should().BeTrue();
        component.LinkedDoorId.Should().Be(linkedDoorId);
        component.OpenAnimation.Should().Be("swing_open");
        component.OpenAngle.Should().Be(120.0f);
    }

    [Fact]
    public void ToEntity_WithPortalComponent_ShouldDeserializeComponent()
    {
        // Arrange
        var destinationWorldId = Guid.NewGuid();
        var destinationLocation = Location.Create(new(100, 200, 300), destinationWorldId, "Portal Map", "Portal Zone");
        var questId1 = Guid.NewGuid();
        var questId2 = Guid.NewGuid();

        var portalComponent = new PortalComponent
        {
            DestinationWorldId = destinationWorldId,
            DestinationZoneId = "portal_zone_456",
            DestinationLocation = destinationLocation,
            RequiresActivation = false,
            IsActivated = true,
            MinimumLevel = 40,
            RequiredQuestIds = new List<Guid> { questId1, questId2 }
        };
        var componentData = new ComponentData
        {
            Type = nameof(PortalComponent),
            Data = JsonSerializer.Serialize(portalComponent)
        };

        var document = new MapObjectDocument
        {
            Id = Guid.NewGuid(),
            Name = "teleport_portal",
            Location = new LocationData { X = 0, Y = 0, Z = 0, WorldId = Guid.NewGuid().ToString(), MapId = "Map", ZoneName = "Zone" },
            WorldId = Guid.NewGuid(),
            ZoneId = "zone_" + Guid.NewGuid().ToString().Substring(0, 8),
            Tags = new List<string>(),
            Components = new List<ComponentData> { componentData }
        };

        // Act
        var mapObject = _mapper.ToEntity(document);

        // Assert
        mapObject.Components.Should().HaveCount(1);
        var component = mapObject.Components[0] as PortalComponent;
        component.Should().NotBeNull();
        component!.DestinationWorldId.Should().Be(destinationWorldId);
        component.DestinationZoneId.Should().Be("portal_zone_456");
        component.MinimumLevel.Should().Be(40);
        component.RequiredQuestIds.Should().HaveCount(2);
        component.RequiredQuestIds.Should().Contain(questId1);
    }

    [Fact]
    public void ToEntity_WithInteractionComponent_ShouldDeserializeComponent()
    {
        // Arrange
        var interactionComponent = new InteractionComponent
        {
            IsInteractable = true,
            InteractionRadius = 10.0f,
            InteractionPrompt = "Search",
            InteractionDurationMs = 3000,
            RequiresLineOfSight = false,
            MaxSimultaneousUsers = 5,
            CooldownSeconds = 60
        };
        var componentData = new ComponentData
        {
            Type = nameof(InteractionComponent),
            Data = JsonSerializer.Serialize(interactionComponent)
        };

        var document = new MapObjectDocument
        {
            Id = Guid.NewGuid(),
            Name = "search_area",
            Location = new LocationData { X = 0, Y = 0, Z = 0, WorldId = Guid.NewGuid().ToString(), MapId = "Map", ZoneName = "Zone" },
            WorldId = Guid.NewGuid(),
            ZoneId = "zone_" + Guid.NewGuid().ToString().Substring(0, 8),
            Tags = new List<string>(),
            Components = new List<ComponentData> { componentData }
        };

        // Act
        var mapObject = _mapper.ToEntity(document);

        // Assert
        mapObject.Components.Should().HaveCount(1);
        var component = mapObject.Components[0] as InteractionComponent;
        component.Should().NotBeNull();
        component!.IsInteractable.Should().BeTrue();
        component.InteractionRadius.Should().Be(10.0f);
        component.InteractionPrompt.Should().Be("Search");
        component.MaxSimultaneousUsers.Should().Be(5);
    }

    [Fact]
    public void ToEntity_WithMultipleComponents_ShouldDeserializeAll()
    {
        // Arrange
        var components = new List<ComponentData>
        {
            new() { Type = nameof(LockableComponent), Data = JsonSerializer.Serialize(new LockableComponent { IsLocked = false }) },
            new() { Type = nameof(DoorComponent), Data = JsonSerializer.Serialize(new DoorComponent { IsOpen = true }) },
            new() { Type = nameof(InteractionComponent), Data = JsonSerializer.Serialize(new InteractionComponent { InteractionPrompt = "Open" }) }
        };

        var document = new MapObjectDocument
        {
            Id = Guid.NewGuid(),
            Name = "multi_component_object",
            Location = new LocationData { X = 0, Y = 0, Z = 0, WorldId = Guid.NewGuid().ToString(), MapId = "Map", ZoneName = "Zone" },
            WorldId = Guid.NewGuid(),
            ZoneId = "zone_" + Guid.NewGuid().ToString().Substring(0, 8),
            Tags = new List<string>(),
            Components = components
        };

        // Act
        var mapObject = _mapper.ToEntity(document);

        // Assert
        mapObject.Components.Should().HaveCount(3);
        mapObject.Components.OfType<LockableComponent>().Should().HaveCount(1);
        mapObject.Components.OfType<DoorComponent>().Should().HaveCount(1);
        mapObject.Components.OfType<InteractionComponent>().Should().HaveCount(1);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveMapObjectData()
    {
        // Arrange
        var location = Location.Create(new(50, 100, 150), Guid.NewGuid(), "TestMap", "TestZone");
        var mapObject = MapObject.Create("test_object", location, Guid.NewGuid(), "zone_" + Guid.NewGuid().ToString().Substring(0, 8));
        mapObject.DisplayName = "Test Object";
        mapObject.Description = "For testing";
        mapObject.Tags = new HashSet<string> { "test" };
        mapObject.Components.Add(new LockableComponent { IsLocked = true });
        mapObject.Components.Add(new InteractionComponent { InteractionPrompt = "Use" });
    mapObject.State["lockState"] = "closed";
    var expectedTimestamp = DateTime.UtcNow.AddHours(-1);
    mapObject.LastUpdated = expectedTimestamp;

        // Act
        var document = _mapper.ToPersistence(mapObject);
        var roundTrippedObject = _mapper.ToEntity(document);

        // Assert
        roundTrippedObject.Name.Should().Be(mapObject.Name);
        roundTrippedObject.DisplayName.Should().Be(mapObject.DisplayName);
        roundTrippedObject.Description.Should().Be(mapObject.Description);
        roundTrippedObject.Components.Should().HaveCount(2);
    roundTrippedObject.State.Should().ContainKey("lockState");
    roundTrippedObject.State["lockState"].Should().Be("closed");
    roundTrippedObject.LastUpdated.Should().Be(expectedTimestamp);
    }
}
