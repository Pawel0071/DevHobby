using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using RabbitMQ.Client;
using RPG.Domain.Common;
using RPG.Infrastructure.Common;
using RPG.Infrastructure.Configuration;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.HealthChecks;
using RPG.Infrastructure.Helpers;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Logger;
using RPG.Infrastructure.OpenTelemetry;
using RPG.Infrastructure.Repositories;
using RPG.Infrastructure.Repositories.Orchestrators;
using RPG.Infrastructure.Repositories.RabbitMQ;
using RPG.Infrastructure.Repositories.Redis;
using RPG.Infrastructure.Repositories.MongoDB;
using RPG.Infrastructure.Mappers;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Skills;
using RPG.Domain.Entities.Quests;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Entities.MapObjects;

namespace RPG.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config,
        string? clientProvidedName = null)
    {
        var rabbitConfig = config.GetSection("RabbitMQ").Get<RabbitMqSettings>();
        var redisConn = config.GetConnectionString("Redis");
        var mongoConn = config.GetConnectionString("Mongo");

        var resolvedClientProvidedName = clientProvidedName
            ?? config.GetValue<string>("RabbitMQ:ClientProvidedName")
            ?? config.GetValue<string>("ApplicationName")
            ?? AppDomain.CurrentDomain.FriendlyName;

        // Logger - Konfiguracja Seriloga
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .Enrich.FromLogContext()
            .CreateLogger();

        services.AddSingleton(typeof(ILogger<>), typeof(SerilogWrapper<>));
        services.AddSingleton<IActivityScope, OpenTelemetryActivityScope>();

        // OpenTelemetry - Telemetria wspólna dla usług infrastruktury
        var otlpEndpoint = config.GetValue<string>("OpenTelemetry:OtlpEndpoint");
    var serviceName = config.GetValue<string>("OpenTelemetry:ServiceName")
              ?? clientProvidedName
              ?? "RPG.GameServer";
        var serviceVersion = config.GetValue<string>("OpenTelemetry:ServiceVersion") ?? "1.0.0";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(serviceName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddGrpcClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                }
            })
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddPrometheusExporter());

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConn!));
        services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        services.AddSingleton<IRedisRepository, RedisRepository>();

        // RabbitMQ
        if (rabbitConfig?.Host != null)
        {
            services.AddSingleton(rabbitConfig);

            services.AddSingleton<IConnection>(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = rabbitConfig.Host,
                    Port = rabbitConfig.Port,
                    UserName = rabbitConfig.Username,
                    Password = rabbitConfig.Password,
                    VirtualHost = rabbitConfig.VirtualHost
                };

                if (!string.IsNullOrWhiteSpace(resolvedClientProvidedName))
                {
                    factory.ClientProvidedName = resolvedClientProvidedName;
                }
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddSingleton<IChannel>(sp =>
            {
                var connection = sp.GetRequiredService<IConnection>();
                return connection.CreateChannelAsync().GetAwaiter().GetResult();
            });

            services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
            services.AddSingleton<IRabbitMqConsumer, RabbitMqConsumer>();
        }
        else
        {
            // Null object pattern when RabbitMQ is not configured
            services.AddSingleton<IRabbitMqPublisher>(sp =>
            {
                var logger = sp.GetService<ILogger<NullRabbitMqPublisher>>();
                return new NullRabbitMqPublisher(logger!);
            });
        }

        // Dictionary Repositories - for loading definitions from MongoDB
        services.AddSingleton<IDictionaryRepository<TagDefinition>, DictionaryRepository<TagDefinition>>();
        services.AddSingleton<IDictionaryRepository<ErrorCodeDefinition>, DictionaryRepository<ErrorCodeDefinition>>();

        // Dictionary Registries - in-memory cache for loaded dictionaries
        services.AddSingleton<IDictionaryRegistry<TagDefinition>, DictionaryRegistry<TagDefinition>>();
        services.AddSingleton<IDictionaryRegistry<ErrorCodeDefinition>, DictionaryRegistry<ErrorCodeDefinition>>();

        // MongoDB
        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConn));
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase("rpg");
        });

        services.AddSingleton<IMongoRepository, MongoRepository>();
        services.AddSingleton<IModelRepository, ModelRepository>();
        services.AddSingleton<IDocumentTypeResolver, DocumentTypeResolver>();

        // Document mappers and supporting helpers
        services.AddSingleton<LocationMapper>();
        services.AddSingleton<IModelMapper<Character, CharacterDocument>, CharacterModelMapper>();
        services.AddSingleton<IModelMapper<Item, ItemDocument>, ItemModelMapper>();
        services.AddSingleton<IModelMapper<Skill, SkillDocument>, SkillModelMapper>();
        services.AddSingleton<IModelMapper<Quest, QuestDocument>, QuestModelMapper>();
        services.AddSingleton<IModelMapper<Npc, NpcDocument>, NpcModelMapper>();
        services.AddSingleton<IModelMapper<GameSession, GameSessionDocument>, GameSessionModelMapper>();
        services.AddSingleton<IModelMapper<Player, PlayerDocument>, PlayerModelMapper>();
        services.AddSingleton<IModelMapper<MapObject, MapObjectDocument>, MapObjectModelMapper>();
        services.AddSingleton<IModelMapper<WorldState, WorldStateDocument>, WorldStateModelMapper>();

        // Health Checks
        services.AddHealthChecks()
            .AddCheck<MongoHealthCheck>("mongo")
            .AddCheck<RedisHealthCheck>("redis")
            .AddCheck<RabbitMqHealthCheck>("rabbitmq");

        // Dictionary warmup hosted service - ensures dictionaries are loaded once per host startup
        services.AddHostedService<DictionaryWarmupService>();

        return services;
    }
}
