using MongoDB.Driver;
using RabbitMQ.Client;
using StackExchange.Redis;
using Testcontainers.MongoDb;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace RPG.IntegrationTests;

public class TestContainersFixture : IAsyncLifetime
{
    private MongoDbContainer? _mongoContainer;
    private RedisContainer? _redisContainer;
    private RabbitMqContainer? _rabbitMqContainer;

    public IMongoClient MongoClient { get; private set; } = null!;
    public IMongoDatabase MongoDatabase { get; private set; } = null!;
    public IConnectionMultiplexer RedisConnection { get; private set; } = null!;
    public IDatabase RedisDatabase { get; private set; } = null!;
    public IConnection RabbitConnection { get; private set; } = null!;
    public IChannel RabbitChannel { get; private set; } = null!;

    public string MongoConnectionString => _mongoContainer?.GetConnectionString() ?? string.Empty;
    public string RedisConnectionString => _redisContainer?.GetConnectionString() ?? string.Empty;
    public string RabbitConnectionString => _rabbitMqContainer?.GetConnectionString() ?? string.Empty;

    public async Task InitializeAsync()
    {
        // Start MongoDB container
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:latest")
            .Build();
        await _mongoContainer.StartAsync();

        // Start Redis container
        _redisContainer = new RedisBuilder()
            .WithImage("redis:latest")
            .Build();
        await _redisContainer.StartAsync();

        // Start RabbitMQ container
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:4-management")
            .Build();
        await _rabbitMqContainer.StartAsync();

        // Initialize MongoDB client
        MongoClient = new MongoClient(MongoConnectionString);
        MongoDatabase = MongoClient.GetDatabase("rpg_test");

        // Initialize Redis client
        RedisConnection = await ConnectionMultiplexer.ConnectAsync(RedisConnectionString);
        RedisDatabase = RedisConnection.GetDatabase();

        // Initialize RabbitMQ client
        var factory = new ConnectionFactory
        {
            Uri = new Uri(RabbitConnectionString)
        };
        RabbitConnection = await factory.CreateConnectionAsync();
        RabbitChannel = await RabbitConnection.CreateChannelAsync();
    }

    public async Task DisposeAsync()
    {
        // Dispose clients
        if (RabbitChannel != null)
            await RabbitChannel.CloseAsync();
        
        if (RabbitConnection != null)
            await RabbitConnection.CloseAsync();

        RedisConnection?.Dispose();

        // Stop containers
        if (_rabbitMqContainer != null)
            await _rabbitMqContainer.DisposeAsync();

        if (_redisContainer != null)
            await _redisContainer.DisposeAsync();

        if (_mongoContainer != null)
            await _mongoContainer.DisposeAsync();
    }
}
