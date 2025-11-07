using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Rabbit;
using RPG.Infrastructure.Repositories;
using RPG.PersistenceService.Adapters;

namespace RPG.IntegrationTests;

/// <summary>
/// End-to-End tests for PersistenceService: RabbitMQ → Infrastructure → MongoDB
/// </summary>
public class PersistenceServiceE2ETests : IClassFixture<TestContainersFixture>, IAsyncLifetime
{
    private readonly TestContainersFixture _fixture;
    private readonly IChannel _rabbitChannel;
    private readonly IMongoDatabase _mongoDatabase;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _exchangeName = "test_rpg_exchange";
    private readonly string _queueName = "test_rpg_persistence_queue";

    public PersistenceServiceE2ETests(TestContainersFixture fixture)
    {
        _fixture = fixture;
        _rabbitChannel = _fixture.RabbitChannel;
        _mongoDatabase = _fixture.MongoDatabase;

        // Setup DI container for Infrastructure services
        var services = new ServiceCollection();
        
        // Logging
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(typeof(RPG.Infrastructure.Interfaces.ILogger<>), typeof(LoggerAdapter<>));
        
        // MongoDB
        services.AddSingleton(_mongoDatabase);
        
        // RabbitMQ
        services.AddSingleton(_rabbitChannel);
        services.AddSingleton(new RabbitMqSettings
        {
            ExchangeName = _exchangeName,
            QueueName = _queueName,
            RoutingKey = "#"
        });
        
        // Infrastructure services
        services.AddSingleton<IDocumentRepository, DocumentRepository>();
        services.AddSingleton<IRabbitMqConsumer, GenericRabbitMqConsumer>();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        // Declare exchange and queue for testing
        await _rabbitChannel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        await _rabbitChannel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await _rabbitChannel.QueueBindAsync(
            queue: _queueName,
            exchange: _exchangeName,
            routingKey: "#");
    }

    public async Task DisposeAsync()
    {
        // Cleanup
        try
        {
            await _rabbitChannel.QueueDeleteAsync(_queueName);
            await _rabbitChannel.ExchangeDeleteAsync(_exchangeName);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task ShouldProcessCharacterCreatedMessage()
    {
        // Arrange
        var characterId = Guid.NewGuid();
        var character = new
        {
            Id = characterId,
            Name = "TestHero",
            Level = 10,
            Health = 100,
            CreatedAt = DateTime.UtcNow
        };

        var message = JsonSerializer.Serialize(character);
        var body = Encoding.UTF8.GetBytes(message);

        var consumer = _serviceProvider.GetRequiredService<IRabbitMqConsumer>();
        var cts = new CancellationTokenSource();

        // Act
        // Start consumer
        var consumerTask = Task.Run(async () => await consumer.StartConsumingAsync(cts.Token));
        await Task.Delay(1000); // Wait for consumer to start

        // Publish message
        await _rabbitChannel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "character.created",
            body: body);

        // Wait for message processing
        await Task.Delay(3000);

        // Assert
        var collection = _mongoDatabase.GetCollection<Dictionary<string, JsonElement>>("Characters");
        var filter = Builders<Dictionary<string, JsonElement>>.Filter.Or(
            Builders<Dictionary<string, JsonElement>>.Filter.Eq("Id", characterId),
            Builders<Dictionary<string, JsonElement>>.Filter.Eq("id", characterId)
        );
        
        var result = await collection.Find(filter).FirstOrDefaultAsync();
        
        result.Should().NotBeNull();
        result!["Name"].GetString().Should().Be("TestHero");
        result["Level"].GetInt32().Should().Be(10);
        result["Health"].GetInt32().Should().Be(100);

        // Cleanup
        cts.Cancel();
        await collection.DeleteOneAsync(filter);
    }

    [Fact]
    public async Task ShouldProcessCharacterUpdatedMessage()
    {
        // Arrange
        var characterId = Guid.NewGuid();
        var collection = _mongoDatabase.GetCollection<Dictionary<string, JsonElement>>("Characters");

        // Insert initial document
        var initialDoc = new Dictionary<string, JsonElement>
        {
            ["Id"] = JsonSerializer.SerializeToElement(characterId),
            ["Name"] = JsonSerializer.SerializeToElement("OldName"),
            ["Level"] = JsonSerializer.SerializeToElement(5)
        };
        await collection.InsertOneAsync(initialDoc);

        // Updated character
        var updatedCharacter = new
        {
            Id = characterId,
            Name = "UpdatedName",
            Level = 15,
            Health = 200
        };

        var message = JsonSerializer.Serialize(updatedCharacter);
        var body = Encoding.UTF8.GetBytes(message);

        var consumer = _serviceProvider.GetRequiredService<IRabbitMqConsumer>();
        var cts = new CancellationTokenSource();

        // Act
        var consumerTask = Task.Run(async () => await consumer.StartConsumingAsync(cts.Token));
        await Task.Delay(3000);

        await _rabbitChannel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "character.updated",
            body: body);

        await Task.Delay(3000);

        // Assert
        var filter = Builders<Dictionary<string, JsonElement>>.Filter.Or(
            Builders<Dictionary<string, JsonElement>>.Filter.Eq("Id", characterId),
            Builders<Dictionary<string, JsonElement>>.Filter.Eq("id", characterId)
        );
        
        var result = await collection.Find(filter).FirstOrDefaultAsync();
        
        result.Should().NotBeNull();
        result!["Name"].GetString().Should().Be("UpdatedName");
        result["Level"].GetInt32().Should().Be(15);
        result["Health"].GetInt32().Should().Be(200);

        // Cleanup
        cts.Cancel();
        await collection.DeleteOneAsync(filter);
    }

    [Fact]
    public async Task ShouldProcessCharacterDeletedMessage()
    {
        // Arrange
        var characterId = Guid.NewGuid();
        var collection = _mongoDatabase.GetCollection<Dictionary<string, JsonElement>>("Characters");

        // Insert document to delete
        var doc = new Dictionary<string, JsonElement>
        {
            ["Id"] = JsonSerializer.SerializeToElement(characterId),
            ["Name"] = JsonSerializer.SerializeToElement("ToBeDeleted")
        };
        await collection.InsertOneAsync(doc);

        // Verify it exists
        var initialFilter = Builders<Dictionary<string, JsonElement>>.Filter.Or(
            Builders<Dictionary<string, JsonElement>>.Filter.Eq("Id", characterId),
            Builders<Dictionary<string, JsonElement>>.Filter.Eq("id", characterId)
        );
        (await collection.Find(initialFilter).FirstOrDefaultAsync()).Should().NotBeNull();

        // Delete message
        var deleteMessage = new { Id = characterId };
        var message = JsonSerializer.Serialize(deleteMessage);
        var body = Encoding.UTF8.GetBytes(message);

        var consumer = _serviceProvider.GetRequiredService<IRabbitMqConsumer>();
        var cts = new CancellationTokenSource();

        // Act
        var consumerTask = Task.Run(async () => await consumer.StartConsumingAsync(cts.Token));
        await Task.Delay(3000);

        await _rabbitChannel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "character.deleted",
            body: body);

        await Task.Delay(3000);

        // Assert
        var result = await collection.Find(initialFilter).FirstOrDefaultAsync();
        result.Should().BeNull();

        // Cleanup
        cts.Cancel();
    }

    [Fact]
    public async Task ShouldProcessItemCreatedMessage()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = new
        {
            Id = itemId,
            Name = "Magic Sword",
            Type = "Weapon",
            Damage = 50,
            Rarity = "Legendary"
        };

        var message = JsonSerializer.Serialize(item);
        var body = Encoding.UTF8.GetBytes(message);

        var consumer = _serviceProvider.GetRequiredService<IRabbitMqConsumer>();
        var cts = new CancellationTokenSource();

        // Act
        var consumerTask = Task.Run(async () => await consumer.StartConsumingAsync(cts.Token));
        await Task.Delay(3000);

        await _rabbitChannel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "item.created",
            body: body);

        await Task.Delay(3000);

        // Assert
        var collection = _mongoDatabase.GetCollection<Dictionary<string, JsonElement>>("Items");
        var filter = Builders<Dictionary<string, JsonElement>>.Filter.Or(
            Builders<Dictionary<string, JsonElement>>.Filter.Eq("Id", itemId),
            Builders<Dictionary<string, JsonElement>>.Filter.Eq("id", itemId)
        );
        
        var result = await collection.Find(filter).FirstOrDefaultAsync();
        
        result.Should().NotBeNull();
        result!["Name"].GetString().Should().Be("Magic Sword");
        result["Type"].GetString().Should().Be("Weapon");
        result["Damage"].GetInt32().Should().Be(50);
        result["Rarity"].GetString().Should().Be("Legendary");

        // Cleanup
        cts.Cancel();
        await collection.DeleteOneAsync(filter);
    }

    [Fact]
    public async Task ShouldSaveToOutboxForAudit()
    {
        // Arrange
        var characterId = Guid.NewGuid();
        var character = new
        {
            Id = characterId,
            Name = "AuditTest"
        };

        var message = JsonSerializer.Serialize(character);
        var body = Encoding.UTF8.GetBytes(message);

        var consumer = _serviceProvider.GetRequiredService<IRabbitMqConsumer>();
        var cts = new CancellationTokenSource();

        // Act
        var consumerTask = Task.Run(async () => await consumer.StartConsumingAsync(cts.Token));
        await Task.Delay(3000);

        await _rabbitChannel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "character.created",
            body: body);

        await Task.Delay(3000);

        // Assert - Check Outbox
        var outboxCollection = _mongoDatabase.GetCollection<Dictionary<string, JsonElement>>("OutboxMessages");
        var outboxFilter = Builders<Dictionary<string, JsonElement>>.Filter.Eq("Topic", "character.created");
        var outboxResult = await outboxCollection.Find(outboxFilter).FirstOrDefaultAsync();

        outboxResult.Should().NotBeNull();
        outboxResult!["Topic"].GetString().Should().Be("character.created");
        outboxResult["Sent"].GetBoolean().Should().BeTrue();

        // Cleanup
        cts.Cancel();
        var characterCollection = _mongoDatabase.GetCollection<Dictionary<string, JsonElement>>("Characters");
        await characterCollection.DeleteManyAsync(Builders<Dictionary<string, JsonElement>>.Filter.Eq("Id", characterId));
        await outboxCollection.DeleteOneAsync(outboxFilter);
    }

    [Fact]
    public async Task ShouldHandleMultipleMessagesInParallel()
    {
        // Arrange
        var characters = Enumerable.Range(1, 5).Select(i => new
        {
            Id = Guid.NewGuid(),
            Name = $"Character{i}",
            Level = i * 10
        }).ToList();

        var consumer = _serviceProvider.GetRequiredService<IRabbitMqConsumer>();
        var cts = new CancellationTokenSource();

        // Act
        var consumerTask = Task.Run(async () => await consumer.StartConsumingAsync(cts.Token));
        await Task.Delay(3000);

        // Publish multiple messages
        foreach (var character in characters)
        {
            var message = JsonSerializer.Serialize(character);
            var body = Encoding.UTF8.GetBytes(message);
            
            await _rabbitChannel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: "character.created",
                body: body);
        }

        await Task.Delay(3000);

        // Assert
        var collection = _mongoDatabase.GetCollection<Dictionary<string, JsonElement>>("Characters");
        
        foreach (var character in characters)
        {
            var filter = Builders<Dictionary<string, JsonElement>>.Filter.Or(
                Builders<Dictionary<string, JsonElement>>.Filter.Eq("Id", character.Id),
                Builders<Dictionary<string, JsonElement>>.Filter.Eq("id", character.Id)
            );
            
            var result = await collection.Find(filter).FirstOrDefaultAsync();
            
            result.Should().NotBeNull();
            result!["Name"].GetString().Should().Be(character.Name);
            result["Level"].GetInt32().Should().Be(character.Level);
        }

        // Cleanup
        cts.Cancel();
        foreach (var character in characters)
        {
            var filter = Builders<Dictionary<string, JsonElement>>.Filter.Or(
                Builders<Dictionary<string, JsonElement>>.Filter.Eq("Id", character.Id),
                Builders<Dictionary<string, JsonElement>>.Filter.Eq("id", character.Id)
            );
            await collection.DeleteOneAsync(filter);
        }
    }
}
