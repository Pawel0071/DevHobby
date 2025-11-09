using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FluentAssertions;
using Moq;
using RPG.Domain.Entities;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Enums;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Mappers;

public class WorldStateDocumentMapperTests
{
    private readonly Mock<ILogger<WorldStateDocumentMapper>> _logger = new();
    private readonly WorldStateDocumentMapper _mapper;

    public WorldStateDocumentMapperTests()
    {
        _mapper = new WorldStateDocumentMapper(_logger.Object);
    }

    [Fact]
    public void ToDocument_ShouldMapAllFields()
    {
        var worldId = Guid.NewGuid();
        var expectedId = Guid.NewGuid();
        var timestamp = new DateTime(2024, 5, 1, 12, 0, 0, DateTimeKind.Utc);

        var hero = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "Hero",
            SessionId = Guid.NewGuid(),
            IsOnline = true,
            IsInCombat = false,
            LastUpdated = timestamp,
            StatusEffects = new HashSet<string> { "buff" }
        };
        var heroLocation = new Location
        {
            Position = new Vector3(10, 5, 0),
            WorldId = worldId,
            MapId = "map",
            ZoneName = "zone",
            Rotation = 90
        };
        hero.SetCurrentLocation(heroLocation);

        var npc = Npc.Create("trainer", "Trainer", Location.Create(new Vector3(12, 5, 0), worldId, "map", "zone"), worldId,
            new HashSet<string> { "vendor" });
        typeof(Npc).GetProperty("Id")!.SetValue(npc, Guid.NewGuid());
        npc.IsAlive = true;
        npc.LastUpdated = timestamp;

        var mapObject = MapObject.Create("chest", Location.Create(new Vector3(8, 3, 0), worldId, "map", "zone"), worldId, "zone");
        typeof(MapObject).GetProperty("Id")!.SetValue(mapObject, Guid.NewGuid());
        mapObject.DisplayName = "Chest";
        mapObject.IsActive = true;
        mapObject.LastUpdated = timestamp;
        mapObject.Tags = new HashSet<string> { "loot" };
        mapObject.State = new Dictionary<string, string> { { "lootTable", "starter" } };

        var entity = WorldState.Hydrate(
            expectedId,
            worldId,
            "Eora",
            timestamp,
            new[] { hero },
            new[] { npc },
            new[] { mapObject });

        var document = _mapper.ToDocument(entity);

        document.Id.Should().Be(expectedId);
        document.WorldId.Should().Be(worldId);
        document.WorldName.Should().Be("Eora");
        document.LastUpdated.Should().Be(entity.LastUpdated);
        document.Characters.Should().HaveCount(1);
        document.Npcs.Should().HaveCount(1);
        document.MapObjects.Should().HaveCount(1);
        document.Characters[0].DisplayName.Should().Be("Hero");
        document.MapObjects[0].State.Should().ContainKey("lootTable");
        _logger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("Converting WorldState"))), Times.Once);
    }

    [Fact]
    public void ToEntity_ShouldMapAllFields()
    {
        var characterId = Guid.NewGuid();
        var mapObjectId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var timestamp = new DateTime(2024, 6, 2, 8, 30, 0, DateTimeKind.Utc);

        var document = new WorldStateDocument
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            WorldName = "Eora",
            LastUpdated = timestamp,
            Characters = new List<WorldCharacterStateDocument>
            {
                new()
                {
                    CharacterId = characterId,
                    SessionId = Guid.NewGuid(),
                    DisplayName = "Hero",
                    Location = new WorldLocationDocument
                    {
                        X = 10,
                        Y = 5,
                        Z = 0,
                        WorldId = worldId,
                        MapId = "map",
                        ZoneName = "zone",
                        Rotation = 180
                    },
                    IsOnline = true,
                    IsInCombat = true,
                    LastUpdated = timestamp,
                    StatusEffects = new HashSet<string> { "dot" }
                }
            },
            Npcs = new List<WorldNpcStateDocument>
            {
                new()
                {
                    NpcId = Guid.NewGuid(),
                    Name = "Trainer",
                    Location = new WorldLocationDocument
                    {
                        X = 9,
                        Y = 5,
                        Z = 0,
                        WorldId = worldId,
                        MapId = "map",
                        ZoneName = "zone",
                        Rotation = 0
                    },
                    IsAlive = true,
                    LastUpdated = timestamp,
                    RespawnAt = timestamp.AddMinutes(5),
                    Tags = new HashSet<string> { "trainer" }
                }
            },
            MapObjects = new List<WorldMapObjectStateDocument>
            {
                new()
                {
                    MapObjectId = mapObjectId,
                    Name = "altar",
                    DisplayName = "Ancient Altar",
                    Location = new WorldLocationDocument
                    {
                        X = 3,
                        Y = 7,
                        Z = 0,
                        WorldId = worldId,
                        MapId = "map",
                        ZoneName = "zone",
                        Rotation = 90
                    },
                    IsActive = false,
                    LastUpdated = timestamp,
                    Tags = new HashSet<string> { "ritual" },
                    State = new Dictionary<string, string> { { "lastActivator", "Hero" } }
                }
            }
        };

        var entity = _mapper.ToEntity(document);

        entity.Id.Should().Be(document.Id);
        entity.WorldId.Should().Be(document.WorldId);
        entity.WorldName.Should().Be("Eora");
        entity.LastUpdated.Should().Be(document.LastUpdated);
    entity.Characters.Should().ContainSingle(c => c.Id == characterId && c.IsInCombat);
    entity.MapObjects.Should().ContainSingle(o => o.Id == mapObjectId && !o.IsActive);
        entity.MapObjects.First().State.Should().ContainKey("lastActivator");
        _logger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("WorldStateDocument"))), Times.Once);
    }
}
