using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RabbitMQ.Client;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Logger;
using RPG.Infrastructure.Outbox;
using RPG.Infrastructure.Rabbit;
using RPG.Infrastructure.Redis;
using StackExchange.Redis;

namespace RPG.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var rabbitConfig = config.GetSection("RabbitMQ").Get<RabbitMqSettings>();
        var redisConn = config.GetConnectionString("Redis");
        var mongoConn = config.GetConnectionString("Mongo");
        
        // Logger
        services.AddSingleton(typeof(ILogger<>), typeof(SerilogWrapper<>));
        
        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConn!));
        services.AddSingleton<IRedisCache, RedisCache>();

        services.AddSingleton<Task<IConnection>>(async sp =>
        {
            if (rabbitConfig?.Host != null)
            {
                var factory = new ConnectionFactory
                {
                    HostName = rabbitConfig!.Host,
                    Port = rabbitConfig!.Port,
                    UserName = rabbitConfig.Username,
                    Password = rabbitConfig.Password,
                    VirtualHost = rabbitConfig.VirtualHost
                };
                return await factory.CreateConnectionAsync();
            }

            return null!;
        });

        services.AddSingleton<Task<IChannel>>(async sp =>
        {
            var connection = await sp.GetRequiredService<Task<IConnection>>();
            return await connection.CreateChannelAsync();
        });

        services.AddSingleton<IRabbitPublisher>(sp =>
        {
            var channelTask = sp.GetRequiredService<Task<IChannel>>();
            var channel = channelTask.GetAwaiter().GetResult(); // wymuszenie synchronizacji
            var logger = sp.GetRequiredService<ILogger<RabbitPublisher>>();
            return new RabbitPublisher(channel, logger);
        });

        // MongoDB
        services.AddSingleton<IMongoCollection<ItemDocument>>(sp =>
        {
            var client = new MongoClient(mongoConn);
            var db = client.GetDatabase("rpg");
            return db.GetCollection<ItemDocument>(ItemDocument.ItemCollection);
        });

        services.AddHostedService<OutboxDispatcher>();
        
        return services;
    }
}
