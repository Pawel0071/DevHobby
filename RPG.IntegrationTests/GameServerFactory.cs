using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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
                ["RabbitMQ:Host"] = rabbitSettings.Host ?? string.Empty,
                ["RabbitMQ:Port"] = rabbitSettings.Port ?? string.Empty,
                ["RabbitMQ:Username"] = rabbitSettings.Username ?? string.Empty,
                ["RabbitMQ:Password"] = rabbitSettings.Password ?? string.Empty,
                ["RabbitMQ:VirtualHost"] = rabbitSettings.VirtualHost ?? "/"
            };

            configBuilder.AddInMemoryCollection(overrides!);
        });

        builder.UseSetting("ConnectionStrings:Mongo", _fixture.MongoConnectionString);
        builder.UseSetting("ConnectionStrings:Redis", redisConnection ?? string.Empty);

        if (!string.IsNullOrEmpty(rabbitSettings.Host)) builder.UseSetting("RabbitMQ:Host", rabbitSettings.Host);
        if (!string.IsNullOrEmpty(rabbitSettings.Port)) builder.UseSetting("RabbitMQ:Port", rabbitSettings.Port);
        if (!string.IsNullOrEmpty(rabbitSettings.Username)) builder.UseSetting("RabbitMQ:Username", rabbitSettings.Username);
        if (!string.IsNullOrEmpty(rabbitSettings.Password)) builder.UseSetting("RabbitMQ:Password", rabbitSettings.Password);
        if (!string.IsNullOrEmpty(rabbitSettings.VirtualHost)) builder.UseSetting("RabbitMQ:VirtualHost", rabbitSettings.VirtualHost);
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
