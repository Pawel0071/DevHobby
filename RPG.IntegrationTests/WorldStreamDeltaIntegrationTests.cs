using System.Numerics;
using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.MapObjects;
using RPG.GameServer.Protos;
using RPG.Infrastructure.Interfaces;
using CharacterClass = RPG.Domain.Enums.CharacterClass;
using DomainLocation = RPG.Domain.Models.Location;
using DomainCharacter = RPG.Domain.Models.Character;
using WorldState = RPG.Domain.Models.WorldState;

namespace RPG.IntegrationTests;

public class WorldStreamDeltaIntegrationTests : IClassFixture<TestContainersFixture>
{
    private readonly TestContainersFixture _fixture;

    public WorldStreamDeltaIntegrationTests(TestContainersFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StreamWorldState_Should_Return_Snapshot_And_Delta()
    {
        await using var factory = new GameServerFactory(_fixture);
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IModelRepository>();
        var broadcaster = scope.ServiceProvider.GetRequiredService<IGameStateBroadcaster>();
        var sessionManager = scope.ServiceProvider.GetRequiredService<RPG.Application.Managers.ISessionManager>();

        var worldId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        // Create active session
        var session = await sessionManager.CreateAsync(playerId, characterId, "127.0.0.1", "test-region", "1.0.0", default);
        var sessionId = session.Id;

        var character = new DomainCharacter(sessionId, CharacterClass.Warrior)
        {
            Id = characterId,
            Name = "TestCharacter"
        };
        character.SetCurrentLocation(new DomainLocation
        {
            Position = Vector3.Zero,
            WorldId = worldId,
            MapId = "map-stream",
            ZoneName = "zone-stream"
        });
        await repo.UpsertAsync(character);

        await SeedWorldStateAsync(repo, worldId, new[] { characterId }, null, null);

        var metadata = new Metadata
        {
            { "x-session-id", sessionId.ToString() }
        };

        var client = new WorldService.WorldServiceClient(factory.CreateGrpcChannel());

        using var call = client.StreamWorldState(new WorldStreamRequest
        {
            SessionId = sessionId.ToString(),
            WorldId = worldId.ToString(),
            IntervalMilliseconds = 100
        }, metadata);

        var newLocation = new DomainLocation
        {
            Position = new Vector3(10, 0, 5),
            WorldId = worldId,
            MapId = "map-stream",
            ZoneName = "zone-stream"
        };

        var deltaUpdate = new GameDeltaUpdate
        {
            WorldId = worldId,
            CharacterChanges =
            [
                new CharacterDelta
                {
                    CharacterId = characterId,
                    Location = newLocation,
                    IsOnline = true
                }
            ]
        };

        await broadcaster.BroadcastDeltaAsync(deltaUpdate);

        WorldUpdate? snapshotUpdate = null;
        WorldUpdate? deltaUpdateMsg = null;

        for (var i = 0; i < 20 && await call.ResponseStream.MoveNext(); i++)
        {
            var current = call.ResponseStream.Current;
            if (snapshotUpdate == null && current.Snapshot is not null)
            {
                snapshotUpdate = current;
            }

            if (current.Delta != null && current.Delta.Characters.Count > 0)
            {
                deltaUpdateMsg = current;
                break;
            }
        }

        snapshotUpdate.Should().NotBeNull("stream powinien zwrócić przynajmniej jeden snapshot świata");
        deltaUpdateMsg.Should().NotBeNull("po opublikowaniu delty powinniśmy ją zobaczyć w streamie");

        // snapshot powinien zawierać postać w pozycji startowej (0,0,0)
        var snapshotChar = snapshotUpdate!.Snapshot.Characters.FirstOrDefault(pc => pc.CharacterId == characterId.ToString());
        snapshotChar.Should().NotBeNull();
        snapshotChar!.Location.Should().NotBeNull();
        snapshotChar.Location.X.Should().Be(0f);
        snapshotChar.Location.Y.Should().Be(0f);
        snapshotChar.Location.Z.Should().Be(0f);

        // delta powinna zawierać nową pozycję (10,0,5)
        var deltaCharacter = deltaUpdateMsg!.Delta.Characters[0];
        deltaCharacter.Location.Should().NotBeNull();
        deltaCharacter.Location!.X.Should().Be(10f);
        deltaCharacter.Location.Z.Should().Be(5f);
        deltaCharacter.Location.WorldId.Should().Be(worldId.ToString());
        deltaCharacter.Location.MapId.Should().Be("map-stream");
    }

    [Fact]
    public async Task StreamWorldState_Should_Return_NpcDelta_And_MapObjectDelta()
    {
        await using var factory = new GameServerFactory(_fixture);
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IModelRepository>();
        var broadcaster = scope.ServiceProvider.GetRequiredService<IGameStateBroadcaster>();
        var sessionManager = scope.ServiceProvider.GetRequiredService<RPG.Application.Managers.ISessionManager>();

        var worldId = Guid.NewGuid();
        var npcId = Guid.NewGuid();
        var mapObjectId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var dummyCharId = Guid.NewGuid();

        // Create active session
        var session = await sessionManager.CreateAsync(playerId, dummyCharId, "127.0.0.1", "test-region", "1.0.0", default);
        var sessionId = session.Id;

        // Seed NPC
        var npc = Npc.Create("TestNPC", "Test NPC", new DomainLocation
        {
            Position = Vector3.Zero,
            WorldId = worldId,
            MapId = "map-npc",
            ZoneName = "zone-npc"
        }, worldId);
        npc.GetType().GetProperty("Id")!.SetValue(npc, npcId);
        await repo.UpsertAsync(npc);

        // Seed MapObject
        var mapObject = MapObject.Create("TestDoor", new DomainLocation
        {
            Position = Vector3.Zero,
            WorldId = worldId,
            MapId = "map-obj",
            ZoneName = "zone-obj"
        }, worldId, "zone-obj");
        mapObject.GetType().GetProperty("Id")!.SetValue(mapObject, mapObjectId);
        mapObject.IsActive = true;
        await repo.UpsertAsync(mapObject);

        await SeedWorldStateAsync(repo, worldId, null, new[] { npcId }, new[] { mapObjectId });

        var metadata = new Metadata
        {
            { "x-session-id", sessionId.ToString() }
        };

        var client = new WorldService.WorldServiceClient(factory.CreateGrpcChannel());

        using var call = client.StreamWorldState(new WorldStreamRequest
        {
            SessionId = sessionId.ToString(),
            WorldId = worldId.ToString(),
            IntervalMilliseconds = 100
        }, metadata);

        // Przygotuj delty: NPC zmienia lokalizację, MapObject zostaje dezaktywowany
        var newNpcLocation = new DomainLocation
        {
            Position = new Vector3(5, 0, 3),
            WorldId = worldId,
            MapId = "map-npc",
            ZoneName = "zone-npc"
        };

        var deltaUpdate = new GameDeltaUpdate
        {
            WorldId = worldId,
            NpcChanges =
            [
                new NpcDelta
                {
                    NpcId = npcId,
                    Location = newNpcLocation,
                    IsAlive = true
                }
            ],
            MapObjectChanges =
            [
                new MapObjectDelta
                {
                    MapObjectId = mapObjectId,
                    IsActive = false
                }
            ]
        };

        await broadcaster.BroadcastDeltaAsync(deltaUpdate);

        WorldUpdate? snapshotUpdate = null;
        WorldUpdate? deltaUpdateMsg = null;

        for (var i = 0; i < 20 && await call.ResponseStream.MoveNext(); i++)
        {
            var current = call.ResponseStream.Current;
            if (snapshotUpdate == null && current.Snapshot is not null)
            {
                snapshotUpdate = current;
            }

            if (current.Delta != null && (current.Delta.Npcs.Count > 0 || current.Delta.MapObjects.Count > 0))
            {
                deltaUpdateMsg = current;
                break;
            }
        }

        snapshotUpdate.Should().NotBeNull("stream powinien zwrócić przynajmniej jeden snapshot świata");
        deltaUpdateMsg.Should().NotBeNull("po opublikowaniu delty NPC i MapObject powinniśmy ją zobaczyć");

        // Snapshot: NPC w pozycji startowej (0,0,0)
        var snapshotNpc = snapshotUpdate!.Snapshot.Npcs.FirstOrDefault(n => n.NpcId == npcId.ToString());
        snapshotNpc.Should().NotBeNull();
        snapshotNpc!.Location.Should().NotBeNull();
        snapshotNpc.Location.X.Should().Be(0f);
        snapshotNpc.Location.Y.Should().Be(0f);
        snapshotNpc.Location.Z.Should().Be(0f);

        // Snapshot: MapObject aktywny (IsActive = true)
        var snapshotMapObj = snapshotUpdate.Snapshot.MapObjects.FirstOrDefault(mo => mo.MapObjectId == mapObjectId.ToString());
        snapshotMapObj.Should().NotBeNull();
        snapshotMapObj!.IsActive.Should().BeTrue();

        // Delta: NPC w nowej pozycji (5,0,3)
        deltaUpdateMsg!.Delta.Npcs.Should().HaveCount(1);
        var deltaNpc = deltaUpdateMsg.Delta.Npcs[0];
        deltaNpc.Location.Should().NotBeNull();
        deltaNpc.Location!.X.Should().Be(5f);
        deltaNpc.Location.Y.Should().Be(0f);
        deltaNpc.Location.Z.Should().Be(3f);

        // Delta: MapObject dezaktywowany (IsActive = false)
        deltaUpdateMsg.Delta.MapObjects.Should().HaveCount(1);
        var deltaMapObj = deltaUpdateMsg.Delta.MapObjects[0];
        deltaMapObj.IsActive.Should().BeFalse();
    }

    private static Task SeedWorldStateAsync(
        IModelRepository repository,
        Guid worldId,
        IEnumerable<Guid>? characters,
        IEnumerable<Guid>? npcs,
        IEnumerable<Guid>? mapObjects,
        CancellationToken cancellationToken = default)
    {
        var worldState = WorldState.Hydrate(
            worldId,
            worldId,
            $"integration-world-{worldId:N}",
            DateTime.UtcNow,
            characters,
            npcs,
            mapObjects);

        return repository.UpsertAsync(worldState, cancellationToken);
    }
}
