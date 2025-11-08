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
using RPG.Infrastructure.Repositories.Orchestrators;
using RPG.Infrastructure.Repositories.RabbitMQ;
using RPG.Infrastructure.Repositories.Redis;
using RPG.Infrastructure.Repositories.MongoDB;
using RPG.Infrastructure.Mappers;
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
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var rabbitConfig = config.GetSection("RabbitMQ").Get<RabbitMqSettings>();
        var redisConn = config.GetConnectionString("Redis");
        var mongoConn = config.GetConnectionString("Mongo");

        // Logger - Konfiguracja Seriloga
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .Enrich.FromLogContext()
            .CreateLogger();

        services.AddSingleton(typeof(ILogger<>), typeof(SerilogWrapper<>));
        services.AddSingleton<IActivityScope, OpenTelemetryActivityScope>();

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConn!));
        services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        services.AddSingleton<IRedisDocumentRepository, RedisDocumentRepository>();

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

        services.AddSingleton<IMongoDocumentRepository, MongoDocumentRepository>();
        services.AddSingleton<IDocumentRepository, DocumentRepository>();
        services.AddSingleton<IDocumentTypeResolver, DocumentTypeResolver>();

        // Document mappers and supporting helpers
        services.AddSingleton<LocationMapper>();
        services.AddSingleton<IDocumentMapper<Character, CharacterDocument>, CharacterDocumentMapper>();
        services.AddSingleton<IDocumentMapper<Item, ItemDocument>, ItemDocumentMapper>();
        services.AddSingleton<IDocumentMapper<Skill, SkillDocument>, SkillDocumentMapper>();
        services.AddSingleton<IDocumentMapper<Quest, QuestDocument>, QuestDocumentMapper>();
        services.AddSingleton<IDocumentMapper<Npc, NpcDocument>, NpcDocumentMapper>();
        services.AddSingleton<IDocumentMapper<Player, PlayerDocument>, PlayerDocumentMapper>();
        services.AddSingleton<IDocumentMapper<MapObject, MapObjectDocument>, MapObjectDocumentMapper>();
        services.AddSingleton<IDocumentMapper<WorldState, WorldStateDocument>, WorldStateDocumentMapper>();

        // Health Checks
        services.AddHealthChecks()
            .AddCheck<MongoHealthCheck>("mongo")
            .AddCheck<RedisHealthCheck>("redis")
            .AddCheck<RabbitMqHealthCheck>("rabbitmq");

        return services;
    }
}
