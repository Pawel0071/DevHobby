using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using RabbitMQ.Client;
using RPG.Infrastructure.Configuration;
using RPG.Infrastructure.Repositories.RabbitMQ;
using RPG.Infrastructure.Interfaces;
using StackExchange.Redis;
using RPG.GameServer;
using Grpc.Net.Client;
using Grpc.Core;
using RPG.GameServer.Protos;

namespace RPG.IntegrationTests;

public sealed class GameServerFactory : WebApplicationFactory<IntegrationEntryPoint>
{
    private readonly TestContainersFixture _fixture;

    public GameServerFactory(TestContainersFixture fixture)
    {
        _fixture = fixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var redisConnection = NormalizeRedisConnection(_fixture.RedisConnectionString);
        var rabbitSettings = ParseRabbitSettings(_fixture.RabbitConnectionString);

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var overrides = new Dictionary<string, string>
            {
                ["ConnectionStrings:Mongo"] = _fixture.MongoConnectionString,
                ["ConnectionStrings:Redis"] = redisConnection ?? string.Empty,
                ["RabbitMQ:ConnectionString"] = _fixture.RabbitConnectionString ?? string.Empty,
                ["RabbitMQ:Host"] = rabbitSettings.Host ?? string.Empty,
                ["RabbitMQ:Port"] = rabbitSettings.Port ?? string.Empty,
                ["RabbitMQ:Username"] = rabbitSettings.Username ?? string.Empty,
                ["RabbitMQ:Password"] = rabbitSettings.Password ?? string.Empty,
                ["RabbitMQ:VirtualHost"] = rabbitSettings.VirtualHost ?? "/"
            };

            configBuilder.AddInMemoryCollection(overrides!);
        });

        builder.ConfigureServices((_, services) =>
        {
            OverrideInfrastructureConnections(services);
        });

        builder.UseSetting("ConnectionStrings:Mongo", _fixture.MongoConnectionString);
        builder.UseSetting("ConnectionStrings:Redis", redisConnection ?? string.Empty);

        if (!string.IsNullOrEmpty(rabbitSettings.Host)) builder.UseSetting("RabbitMQ:Host", rabbitSettings.Host);
        if (!string.IsNullOrEmpty(rabbitSettings.Port)) builder.UseSetting("RabbitMQ:Port", rabbitSettings.Port);
        if (!string.IsNullOrEmpty(rabbitSettings.Username)) builder.UseSetting("RabbitMQ:Username", rabbitSettings.Username);
        if (!string.IsNullOrEmpty(rabbitSettings.Password)) builder.UseSetting("RabbitMQ:Password", rabbitSettings.Password);
        if (!string.IsNullOrEmpty(rabbitSettings.VirtualHost)) builder.UseSetting("RabbitMQ:VirtualHost", rabbitSettings.VirtualHost);
        if (!string.IsNullOrEmpty(_fixture.RabbitConnectionString)) builder.UseSetting("RabbitMQ:ConnectionString", _fixture.RabbitConnectionString);
    }

    private void OverrideInfrastructureConnections(IServiceCollection services)
    {
        services.RemoveAll<IMongoClient>();
        services.RemoveAll<IMongoDatabase>();
        services.RemoveAll<IConnectionMultiplexer>();
        services.RemoveAll<IDatabase>();
        services.RemoveAll<RabbitMqSettings>();
        services.RemoveAll<IConnection>();
        services.RemoveAll<IChannel>();
        services.RemoveAll<IRabbitMqPublisher>();
        services.RemoveAll<IRabbitMqConsumer>();

        services.AddSingleton<IMongoClient>(_ => _fixture.MongoClient);
        services.AddSingleton<IMongoDatabase>(_ => _fixture.MongoDatabase);

        services.AddSingleton<IConnectionMultiplexer>(_ => _fixture.RedisConnection);
        services.AddSingleton<IDatabase>(_ => _fixture.RedisDatabase);

        services.AddSingleton(new RabbitMqSettings());

        services.AddSingleton<IRabbitMqPublisher>(_ => new NullRabbitMqPublisher(null));
        services.AddSingleton<IRabbitMqConsumer>(_ => new NoOpRabbitMqConsumer());

        RegisterWorldSeederDependencies(services);
    }

    private static void RegisterWorldSeederDependencies(IServiceCollection services)
    {
        var seedDataLoaderType = Type.GetType("RPG.WorldSeeder.Seeders.SeedDataLoader, RPG.WorldSeeder", throwOnError: false);
        if (seedDataLoaderType != null)
        {
            services.TryAddSingleton(seedDataLoaderType);
        }

        var worldSeederServiceType = Type.GetType("RPG.WorldSeeder.Services.WorldSeederService, RPG.WorldSeeder", throwOnError: false);
        if (worldSeederServiceType != null)
        {
            services.TryAddSingleton(worldSeederServiceType);
        }
    }

    private sealed class NoOpRabbitMqConsumer : IRabbitMqConsumer
    {
        public void SetMessageHandler(Func<string, string, CancellationToken, Task> handler)
        {
        }

        public Task StartConsumingAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StopConsumingAsync()
        {
            return Task.CompletedTask;
        }
    }

    private static string? NormalizeRedisConnection(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        if (!connectionString.Contains("://", StringComparison.Ordinal))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        return uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
    }

    private static (string? Host, string? Port, string? Username, string? Password, string? VirtualHost) ParseRabbitSettings(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (null, null, null, null, null);
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = userInfo.Length > 0 ? userInfo[0] : null;
        var password = userInfo.Length > 1 ? userInfo[1] : null;
        var virtualHost = uri.AbsolutePath.TrimStart('/');
        if (string.IsNullOrEmpty(virtualHost))
        {
            virtualHost = "/";
        }

        return (uri.Host, uri.Port.ToString(), username, password, virtualHost);
    }

    public GrpcChannel CreateGrpcChannel()
    {
        var baseAddress = Server.BaseAddress ?? new Uri("http://localhost");
        var handler = Server.CreateHandler();
        return GrpcChannel.ForAddress(baseAddress, new GrpcChannelOptions { HttpHandler = handler });
    }

    public async Task<(GrpcChannel Channel, Metadata Headers)> CreateAuthenticatedChannelAsync(
        string characterName,
        CancellationToken cancellationToken = default)
    {
        var channel = CreateGrpcChannel();
        try
        {
            var headers = await CreateSessionHeadersAsync(channel, characterName, cancellationToken);
            return (channel, headers);
        }
        catch
        {
            channel.Dispose();
            throw;
        }
    }

    private static async Task<Metadata> CreateSessionHeadersAsync(
        GrpcChannel channel,
        string characterName,
        CancellationToken cancellationToken)
    {
        var characterClient = new CharacterService.CharacterServiceClient(channel);
        var sessionClient = new SessionService.SessionServiceClient(channel);

        var characterReply = await characterClient.CreateCharacterAsync(
            BuildCharacterRequest(characterName),
            cancellationToken: cancellationToken);

        var sessionReply = await sessionClient.CreateSessionAsync(new CreateSessionRequest
        {
            CharacterId = characterReply.CharacterId,
            PlayerId = Guid.NewGuid().ToString()
        }, cancellationToken: cancellationToken);

        return new Metadata
        {
            { "x-session-id", sessionReply.Session.Id }
        };
    }

    private static CharacterRequest BuildCharacterRequest(string name)
    {
        return new CharacterRequest
        {
            Character = new PlayerCharacter
            {
                CharacterClass = CharacterClass.Warrior,
                SessionId = Guid.NewGuid().ToString(),
                BaseCharacter = new BaseCharacter
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Level = 5,
                    MaxHealth = 150,
                    CurrentHealth = 150,
                    MaxMana = 80,
                    CurrentMana = 80,
                    Rotation = 0f,
                    Position = new Location
                    {
                        X = 0,
                        Y = 0,
                        Z = 0,
                        WorldId = Guid.NewGuid().ToString(),
                        MapId = "integration-map",
                        ZoneName = "integration-zone",
                        Rotation = 0f
                    },
                    Stats = new Stats
                    {
                        Strength = 10,
                        Vitality = 10,
                        Intelligence = 5,
                        Wisdom = 4,
                        Dexterity = 6,
                        Agility = 6,
                        MagicResist = 2,
                        NatureResist = 2,
                        FireResist = 2,
                        Armor = 5,
                        CritChance = 1,
                        HitChance = 90,
                        AttackSpeed = 1,
                        MoveSpeed = 5
                    }
                }
            }
        };
    }
}
