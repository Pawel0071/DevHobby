using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using RabbitMQ.Client;
using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Infrastructure.Common;
using RPG.Infrastructure.Configuration;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Logger;
using RPG.Infrastructure.Outbox;
using RPG.Infrastructure.Repositories.MongoDB;
using RPG.Infrastructure.Repositories.RabbitMQ;
using RPG.Infrastructure.Repositories.Redis;
using RPG.Infrastructure.Services;
using Serilog;
using StackExchange.Redis;

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
        
        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConn!));
        services.AddSingleton<IRedisCache, RedisCache>();

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
        }
        else
        {
            // Null object pattern when RabbitMQ is not configured
            services.AddSingleton<IRabbitMqPublisher>(sp => 
            {
                var logger = sp.GetService<ILogger<NullRabbitMqPublisher>>();
                return new NullRabbitMqPublisher(logger);
            });
        }
        
        services.AddScoped<IDictionaryRepository<ItemTagDefinition>, MongoDictionaryRepository<ItemTagDefinition>>();
        services.AddScoped<IDictionaryRepository<ErrorCodeDefinition>, MongoDictionaryRepository<ErrorCodeDefinition>>();
        services.AddScoped<IDictionaryRepository<ItemTypeDefinition>, MongoDictionaryRepository<ItemTypeDefinition>>();

        services.AddSingleton<IDictionaryRegistry<ItemTagDefinition>, DictionaryRegistry<ItemTagDefinition>>(); 
        services.AddSingleton<IDictionaryRegistry<ErrorCodeDefinition>, DictionaryRegistry<ErrorCodeDefinition>>(); 
        services.AddSingleton<IDictionaryRegistry<ItemTypeDefinition>, DictionaryRegistry<ItemTypeDefinition>>();
        
        // MongoDB
        services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConn));
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase("rpg");
        });
        
        services.AddSingleton<IMongoCollection<ItemDocument>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<ItemDocument>(ItemDocument.ItemCollection);
        });
        
        services.AddSingleton<IMongoCollection<OutboxMessage>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            return db.GetCollection<OutboxMessage>("OutboxMessages");
        });

        services.AddSingleton<IHostedService, DictionaryWarmupService>();
        services.AddHostedService<OutboxDispatcher>();
        
        // Health Checks
        services.AddHealthChecks()
            .AddCheck<RPG.Infrastructure.HealthChecks.MongoHealthCheck>("mongodb")
            .AddCheck<RPG.Infrastructure.HealthChecks.RedisHealthCheck>("redis")
            .AddCheck<RPG.Infrastructure.HealthChecks.RabbitMqHealthCheck>("rabbitmq");
        
        return services;
    }
}
