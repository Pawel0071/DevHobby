using MongoDB.Driver;
using PersistenceService;
using RabbitMQ.Client;
using RPG.PersistenceService.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

// MongoDB - read from configuration
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Mongo") ?? "mongodb://localhost:27017";
    var client = new MongoClient(connectionString);
    return client.GetDatabase("rpgdb");
});

// MongoDB Character Collection
builder.Services.AddSingleton(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<RPG.Domain.Entities.Character>("Characters");
});

// RabbitMQ - read from configuration
builder.Services.AddSingleton<IConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var host = config["RabbitMQ:Host"] ?? "localhost";
    var port = int.Parse(config["RabbitMQ:Port"] ?? "5672");
    var username = config["RabbitMQ:Username"] ?? "guest";
    var password = config["RabbitMQ:Password"] ?? "guest";
    var virtualHost = config["RabbitMQ:VirtualHost"] ?? "/";
    
    var factory = new ConnectionFactory 
    { 
        HostName = host,
        Port = port,
        UserName = username,
        Password = password,
        VirtualHost = virtualHost
    };
    
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// RabbitMQ Channel
builder.Services.AddSingleton<IChannel>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    return connection.CreateChannelAsync().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IRabbitMqToMongoService, RabbitMqToMongoService>();

var host = builder.Build();

var rabbitService = host.Services.GetRequiredService<IRabbitMqToMongoService>();
rabbitService.StartListening();

host.Run();