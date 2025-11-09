using FluentAssertions;
using Grpc.Net.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RPG.Domain.Interfaces;
using DomainCharacterRepository = RPG.Domain.Interfaces.ICharacterRepository;
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
    var repository = scope.ServiceProvider.GetRequiredService<DomainCharacterRepository>();
        var character = await repository.GetByIdAsync(characterId);

        character.Name.Should().Be("IntegrationHero");
        character.Level.Should().Be(3);
        character.CurrentLocation.Position.X.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task MovementLifecycle_UpdatesRepositoryState()
    {
        await _fixture.ResetStateAsync();

        using var factory = new GameServerFactory(_fixture);
        _ = factory.CreateClient();

        using var channel = CreateGrpcChannel(factory);
        var client = new CharacterServiceClient(channel);

        var createReply = await client.CreateCharacterAsync(BuildCharacterRequest("MovementHero"));

        var startMove = await client.StartMovementAsync(new MovementCommandRequest
        {
            CharacterId = createReply.CharacterId,
            Direction = 1
        });

        startMove.Success.Should().BeTrue();

        using (var scope = factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<DomainCharacterRepository>();
            var character = await repository.GetByIdAsync(Guid.Parse(createReply.CharacterId));

            character.IsMoving.Should().BeTrue();
            character.CurrentLocation.Position.Z.Should().BeGreaterThan(0f);
        }

        var stopMove = await client.StopMovementAsync(new CharacterIdRequest { CharacterId = createReply.CharacterId });
        stopMove.Success.Should().BeTrue();

        var startRotation = await client.StartRotationAsync(new MovementCommandRequest
        {
            CharacterId = createReply.CharacterId,
            Direction = 3
        });

        startRotation.Success.Should().BeTrue();

        using (var scope = factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<DomainCharacterRepository>();
            var character = await repository.GetByIdAsync(Guid.Parse(createReply.CharacterId));

            character.IsRotating.Should().BeTrue();
            character.CurrentLocation.Rotation.Should().BeApproximately(90f, 0.01f);
        }

        var stopRotation = await client.StopRotationAsync(new CharacterIdRequest { CharacterId = createReply.CharacterId });
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
                    Position = new ProtoLocation { X = 1, Y = 2, Z = 3 }
                }
            }
        };
    }
}
