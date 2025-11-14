using System.Reflection;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using RPG.GameServer.Protos;
using CharacterServiceClient = RPG.GameServer.Protos.CharacterService.CharacterServiceClient;
using ProtoBaseCharacter = RPG.GameServer.Protos.BaseCharacter;
using ProtoCharacterClass = RPG.GameServer.Protos.CharacterClass;
using ProtoPlayerCharacter = RPG.GameServer.Protos.PlayerCharacter;
using ProtoStats = RPG.GameServer.Protos.Stats;
using ProtoLocation = RPG.GameServer.Protos.Location;

namespace RPG.IntegrationTests;

public class WorldSessionGrpcIntegrationTests : IClassFixture<TestContainersFixture>
{
    private static readonly string StarterWorldId = "c2bce5a0-5d6d-4eb5-8f5c-5aeb1b6f6b3d";
    private readonly TestContainersFixture _fixture;

    public WorldSessionGrpcIntegrationTests(TestContainersFixture fixture)
    {
        _fixture = fixture;
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    }

    [Fact]
    public async Task JoinWorld_ReturnsSeededWorldState()
    {
        await _fixture.ResetStateAsync();

        using var factory = new GameServerFactory(_fixture);
        _ = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            await RunSeederAsync(scope.ServiceProvider);
        }

        using var channel = CreateGrpcChannel(factory);
        var characterClient = new CharacterServiceClient(channel);
    var sessionClient = new SessionService.SessionServiceClient(channel);
    var worldClient = new WorldService.WorldServiceClient(channel);

        var characterReply = await characterClient.CreateCharacterAsync(BuildCharacterRequest("WorldJoiner"));
        var sessionReply = await sessionClient.CreateSessionAsync(new CreateSessionRequest
        {
            CharacterId = characterReply.CharacterId,
            PlayerId = Guid.NewGuid().ToString()
        });

        var sessionHeaders = new Metadata
        {
            { "x-session-id", sessionReply.Session.Id }
        };

        var joinReply = await worldClient.JoinWorldAsync(new JoinWorldRequest
        {
            SessionId = sessionReply.Session.Id
        }, sessionHeaders);

        joinReply.SpawnLocation.Should().NotBeNull();
        joinReply.SpawnLocation.WorldId.Should().Be(StarterWorldId);
        joinReply.SpawnLocation.MapId.Should().Be("starter.map");
        joinReply.SpawnLocation.ZoneName.Should().Be("starter.zone");
        joinReply.SpawnLocation.Rotation.Should().BeApproximately(180f, 0.01f);

        var snapshot = joinReply.Snapshot;
        snapshot.Metadata.WorldId.Should().Be(StarterWorldId);
        snapshot.Characters.Should().ContainSingle(c => c.CharacterId == characterReply.CharacterId);
        snapshot.Npcs.Select(n => n.Name).Should().Contain(new[] { "Village Guide", "Goblin Scout", "Goblin Warrior" });
        snapshot.MapObjects.Select(o => o.Name).Should().Contain(new[]
        {
            "starter.spawn.default",
            "starter.market.stall-north",
            "starter.market.stall-south",
            "starter.structure.town-gate",
            "starter.building.townhall"
        });

        var spawnObject = snapshot.MapObjects.Single(o => o.Name == "starter.spawn.default");
        spawnObject.Tags.Should().Contain("spawn-point");
        spawnObject.State.Should().ContainKey("spawnType").WhoseValue.Should().Be("player-default");
    }

    private static async Task RunSeederAsync(IServiceProvider serviceProvider)
    {
        var assembly = Assembly.Load("RPG.WorldSeeder");
        var seederType = assembly.GetType("RPG.WorldSeeder.Services.WorldSeederService", throwOnError: true) ??
                         throw new InvalidOperationException("WorldSeederService type not found.");

        var seeder = ActivatorUtilities.CreateInstance(serviceProvider, seederType);
        var method = seederType.GetMethod("SeedAsync", new[] { typeof(CancellationToken) }) ??
                     seederType.GetMethod("SeedAsync", Type.EmptyTypes) ??
                     throw new InvalidOperationException("SeedAsync method not found on WorldSeederService.");

        object? invocationResult = method.GetParameters().Length == 0
            ? method.Invoke(seeder, Array.Empty<object?>())
            : method.Invoke(seeder, new object?[] { CancellationToken.None });

        if (invocationResult is Task task)
        {
            await task.ConfigureAwait(false);
        }
    }

    private static CharacterRequest BuildCharacterRequest(string name)
    {
        return new CharacterRequest
        {
            Character = new ProtoPlayerCharacter
            {
                CharacterClass = ProtoCharacterClass.Warrior,
                SessionId = Guid.NewGuid().ToString(),
                BaseCharacter = new ProtoBaseCharacter
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Level = 3,
                    MaxHealth = 120,
                    CurrentHealth = 120,
                    MaxMana = 60,
                    CurrentMana = 60,
                    Rotation = 0,
                    Stats = new ProtoStats { MoveSpeed = 5, Strength = 12, Vitality = 10 },
                    Position = new ProtoLocation
                    {
                        X = 5,
                        Y = 3,
                        Z = 0,
                        WorldId = StarterWorldId,
                        MapId = "starter.map",
                        ZoneName = "starter.zone",
                        Rotation = 0
                    }
                }
            }
        };
    }

    private static GrpcChannel CreateGrpcChannel(GameServerFactory factory)
    {
        var baseAddress = factory.Server.BaseAddress ?? new Uri("http://localhost");
        var handler = factory.Server.CreateHandler();

        return GrpcChannel.ForAddress(baseAddress, new GrpcChannelOptions
        {
            HttpHandler = handler
        });
    }
}
