using FluentAssertions;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;
using RPG.GameServer.Protos;
using CharacterServiceClient = RPG.GameServer.Protos.CharacterService.CharacterServiceClient;
using ProtoCharacterClass = RPG.GameServer.Protos.CharacterClass;
using ProtoBaseCharacter = RPG.GameServer.Protos.BaseCharacter;
using ProtoPlayerCharacter = RPG.GameServer.Protos.PlayerCharacter;
using ProtoStats = RPG.GameServer.Protos.Stats;
using ProtoLocation = RPG.GameServer.Protos.Location;

namespace RPG.IntegrationTests;

public class CharacterGrpcIntegrationTests : IClassFixture<TestContainersFixture>
{
    private readonly TestContainersFixture _fixture;

    public CharacterGrpcIntegrationTests(TestContainersFixture fixture)
    {
        _fixture = fixture;
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    }

    [Fact]
    public async Task CreateCharacter_PersistsCharacter()
    {
        await _fixture.ResetStateAsync();

        using var factory = new GameServerFactory(_fixture);
        // Ensure the host starts before creating the gRPC channel.
        _ = factory.CreateClient();

        using var channel = CreateGrpcChannel(factory);
        var client = new CharacterServiceClient(channel);

        var request = BuildCharacterRequest("IntegrationHero");

        var response = await client.CreateCharacterAsync(request);
        response.CharacterId.Should().NotBeNullOrWhiteSpace();

        var characterId = Guid.Parse(response.CharacterId);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IModelRepository>();
        var character = await repository.GetByIdAsync<Character>(characterId);

        character?.Name.Should().Be("IntegrationHero");
        character?.Level.Should().Be(3);
        character?.CurrentLocation.Position.X.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task MovementLifecycle_UpdatesRepositoryState()
    {
        await _fixture.ResetStateAsync();

        using var factory = new GameServerFactory(_fixture);
        _ = factory.CreateClient();

        using var channel = CreateGrpcChannel(factory);
        var client = new CharacterServiceClient(channel);
        var sessionClient = new SessionService.SessionServiceClient(channel);

        var createReply = await client.CreateCharacterAsync(BuildCharacterRequest("MovementHero"));
        var characterId = createReply.CharacterId;

        // Create session before movement commands
        var sessionReply = await sessionClient.CreateSessionAsync(new CreateSessionRequest
        {
            CharacterId = characterId,
            PlayerId = Guid.NewGuid().ToString()
        });

        var headers = new Grpc.Core.Metadata
        {
            { "x-session-id", sessionReply.Session.Id }
        };

        var startMove = await client.StartMovementAsync(new MovementCommandRequest
        {
            CharacterId = characterId,
            Direction = 1
        }, headers);

        startMove.Success.Should().BeTrue();

        // Retry polling until IsMoving = true or timeout
        await AssertWithRetryAsync(factory, Guid.Parse(characterId), c => c.IsMoving, "IsMoving should be true after StartMovement");
        await AssertWithRetryAsync(factory, Guid.Parse(characterId), c => c.CurrentLocation.Position.Y > 0f, "Y position should increase after movement");

        var stopMove = await client.StopMovementAsync(new CharacterIdRequest { CharacterId = characterId }, headers);
        stopMove.Success.Should().BeTrue();

        var startRotation = await client.StartRotationAsync(new MovementCommandRequest
        {
            CharacterId = characterId,
            Direction = 3
        }, headers);

        startRotation.Success.Should().BeTrue();

        await AssertWithRetryAsync(factory, Guid.Parse(characterId), c => c.IsRotating, "IsRotating should be true after StartRotation");
        await AssertWithRetryAsync(factory, Guid.Parse(characterId), c => Math.Abs(c.CurrentLocation.Direction - 90f) < 0.01f, "Direction should approach 90 degrees");

        var stopRotation = await client.StopRotationAsync(new CharacterIdRequest { CharacterId = characterId }, headers);
        stopRotation.Success.Should().BeTrue();
    }

    private static GrpcChannel CreateGrpcChannel(GameServerFactory factory)
    {
        var baseAddress = factory.Server.BaseAddress ?? new Uri("http://localhost");
        var httpHandler = factory.Server.CreateHandler();

        return GrpcChannel.ForAddress(baseAddress, new GrpcChannelOptions
        {
            HttpHandler = httpHandler
        });
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
                    Stats = new ProtoStats { MoveSpeed = 5, Strength = 15, Vitality = 12 },
                    Position = new ProtoLocation
                    {
                        X = 1,
                        Y = 2,
                        Z = 3,
                        WorldId = "c2bce5a0-5d6d-4eb5-8f5c-5aeb1b6f6b3d",
                        MapId = "starter.map",
                        ZoneName = "starter.zone",
                        Rotation = 0
                    }
                }
            }
        };
    }

    private static async Task AssertWithRetryAsync(GameServerFactory factory, Guid characterId, Func<Character, bool> predicate, string message, int maxAttempts = 100, int delayMs = 50)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var scope = factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IModelRepository>();
            var c = await repo.GetByIdAsync<Character>(characterId);
            if (c != null && predicate(c)) return;
            await Task.Delay(delayMs);
        }
        using var finalScope = factory.Services.CreateScope();
        var finalRepo = finalScope.ServiceProvider.GetRequiredService<IModelRepository>();
        var finalCharacter = await finalRepo.GetByIdAsync<Character>(characterId);
        finalCharacter.Should().NotBeNull(message);
        predicate(finalCharacter!).Should().BeTrue(message);
    }
}
