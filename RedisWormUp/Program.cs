using Cache.WormUp;
using Cache.WormUp.Adapters;
using Cache.WormUp.Service;
using MongoDB.Driver;
using RPG.Infrastructure.Configuration;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.MongoDB;
using RPG.Infrastructure.Repositories.Redis;
using RPG.Infrastructure.Services;
using Serilog;
using StackExchange.Redis;

// Konfiguracja Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/redis-warmup-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Redis WarmUp Service");

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

    // Redis
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var connectionString = config.GetConnectionString("Redis") ?? "localhost:6379";
        return ConnectionMultiplexer.Connect(connectionString);
    });

    builder.Services.AddSingleton<IDatabase>(sp =>
    {
        var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
        return multiplexer.GetDatabase();
    });

    // Redis WarmUp Settings
    builder.Services.AddSingleton(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var settings = new RedisWarmUpSettings();
        config.GetSection("RedisWarmUp").Bind(settings);
        return settings;
    });

    // Infrastructure logger adapters
    builder.Services.AddSingleton(typeof(RPG.Infrastructure.Interfaces.ILogger<>), typeof(LoggerAdapter<>));

    // Infrastructure services
    builder.Services.AddSingleton<IMongoDocumentReader, MongoDocumentReader>();
    builder.Services.AddSingleton<IRedisDocumentWriter, RedisDocumentWriter>();
    builder.Services.AddSingleton<IRedisWarmUpService, RedisWarmUpService>();

    // Application service
    builder.Services.AddSingleton<IMongoToRedisService, MongoToRedisService>();

    var host = builder.Build();

    Log.Information("MongoDB connection: {MongoConnection}", 
        builder.Configuration.GetConnectionString("Mongo") ?? "mongodb://localhost:27017");
    Log.Information("Redis connection: {RedisConnection}", 
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");

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
