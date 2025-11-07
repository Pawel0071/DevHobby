using MongoDB.Driver;
using PersistenceService;
using RabbitMQ.Client;
using RPG.Infrastructure.Configuration;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.MongoDB;
using RPG.Infrastructure.Repositories.RabbitMQ;
using RPG.PersistenceService.Adapters;
using RPG.PersistenceService.Service;
using Serilog;

// Konfiguracja Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/persistence-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting RPG Persistence Service");

    var builder = Host.CreateApplicationBuilder(args);

    // Dodaj Serilog
    builder.Services.AddSerilog();

    // Worker service
    builder.Services.AddHostedService<Worker>();

    // MongoDB
    builder.Services.AddSingleton<IMongoDatabase>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var connectionString = config.GetConnectionString("Mongo") ?? "mongodb://localhost:27017";
        var client = new MongoClient(connectionString);
        return client.GetDatabase("rpgdb");
    });

    // RabbitMQ Connection
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

    // RabbitMQ Settings
    builder.Services.AddSingleton(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        return new RabbitMqSettings
        {
            Host = config["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
            Username = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest",
            VirtualHost = config["RabbitMQ:VirtualHost"] ?? "/",
            ExchangeName = "rpg_exchange",
            QueueName = "rpg_persistence_queue",
            RoutingKey = "#"
        };
    });

    // Infrastructure logger adapters
    builder.Services.AddSingleton(typeof(RPG.Infrastructure.Interfaces.ILogger<>), typeof(LoggerAdapter<>));

    // Infrastructure services
    builder.Services.AddSingleton<IDocumentRepository, DocumentRepository>();
    builder.Services.AddSingleton<IRabbitMqConsumer, RabbitMqConsumer>();

    // Application service
    builder.Services.AddSingleton<IRabbitMqToMongoService, RabbitMqToMongoService>();

    var host = builder.Build();

    Log.Information("MongoDB connection: {MongoConnection}", 
        builder.Configuration.GetConnectionString("Mongo") ?? "mongodb://localhost:27017");
    Log.Information("RabbitMQ connection: {RabbitHost}:{RabbitPort}", 
        builder.Configuration["RabbitMQ:Host"] ?? "localhost",
        builder.Configuration["RabbitMQ:Port"] ?? "5672");

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}