using System;
using System.Collections.Generic;
using System.Numerics;
using FluentAssertions;
using Moq;
using RPG.Abstractions.SharedModel;
using RPG.Domain.Models;
using RPG.GameServer.Services;
using RPG.Infrastructure.Interfaces;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Outbox;

public class GameDeltaBufferTests
{
    [Fact]
    public void DequeueAggregated_WithSingleDelta_ShouldReturnExpectedNpcAndCharacterLocations()
    {
        var loggerMock = new Mock<ILogger<GameDeltaBuffer>>();
        var buffer = new GameDeltaBuffer(loggerMock.Object);
        var worldId = Guid.NewGuid();

        var npcLocation = new Location
        {
            Position = new Vector3(1f, 2f, 3f),
            WorldId = worldId,
            MapId = "map-1",
            MapName = "zone-1",
            Direction = 90f
        };

        var characterLocation = new Location
        {
            Position = new Vector3(4f, 5f, 6f),
            WorldId = worldId,
            MapId = "map-2",
            MapName = "zone-2",
            Direction = 180f
        };

        var delta = new GameDeltaUpdate
        {
            WorldId = worldId,
            NpcChanges = new List<NpcDelta>
            {
                new()
                {
                    NpcId = Guid.NewGuid(),
                    Location = npcLocation,
                    IsAlive = true
                }
            },
            CharacterChanges = new List<CharacterDelta>
            {
                new()
                {
                    CharacterId = Guid.NewGuid(),
                    Location = characterLocation,
                    IsOnline = true
                }
            },
            MapObjectChanges = new List<MapObjectDelta>()
        };

        buffer.Enqueue(delta);

        var result = buffer.DequeueAggregated(worldId);

        result.Npcs.Should().HaveCount(1);
        result.Characters.Should().HaveCount(1);

        var npcProto = result.Npcs[0].Location;
        npcProto.Should().NotBeNull();
        npcProto!.X.Should().Be(1f);
        npcProto.Y.Should().Be(2f);
        npcProto.Z.Should().Be(3f);
        npcProto.WorldId.Should().Be(worldId.ToString());
        npcProto.MapId.Should().Be("map-1");
        npcProto.ZoneName.Should().Be("zone-1");
        npcProto.Rotation.Should().Be(90f);

        var characterProto = result.Characters[0].Location;
        characterProto.Should().NotBeNull();
        characterProto!.X.Should().Be(4f);
        characterProto.Y.Should().Be(5f);
        characterProto.Z.Should().Be(6f);
        characterProto.WorldId.Should().Be(worldId.ToString());
        characterProto.MapId.Should().Be("map-2");
        characterProto.ZoneName.Should().Be("zone-2");
        characterProto.Rotation.Should().Be(180f);
    }
}
