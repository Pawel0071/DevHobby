using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client;
using RPG.Infrastructure.Configuration;
using RPG.Infrastructure.Repositories.RabbitMQ;
using RPG.Infrastructure.Interfaces;
using StackExchange.Redis;
using RPG.GameServer;

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

        builder.ConfigureAppConfiguration((context, configBuilder) =>
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

        builder.ConfigureServices((context, services) =>
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
}
