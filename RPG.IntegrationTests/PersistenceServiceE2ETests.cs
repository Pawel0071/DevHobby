using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;
using RabbitMQ.Client;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Configuration;
using RPG.Infrastructure.Repositories.MongoDB;
using RPG.Infrastructure.Repositories.RabbitMQ;
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
        // Declare exchange and queue for testing FIRST
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
        
        // THEN start RabbitMQ consumer in background
        var consumer = _serviceProvider.GetRequiredService<IRabbitMqConsumer>();
        _ = consumer.StartConsumingAsync(); // Fire and forget - runs in background
        
        // Give consumer more time to start and bind properly
        await Task.Delay(5000);
        
        Console.WriteLine($"Consumer started for exchange={_exchangeName}, queue={_queueName}");
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

        // Act
        // Publish message (consumer already running from InitializeAsync)
        await _rabbitChannel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "character.created",
            body: body);

        // Wait for message processing
        await Task.Delay(3000);

        // Assert
        var collection = _mongoDatabase.GetCollection<BsonDocument>("Characters");
        var filter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Eq("Id", characterId.ToString()),
            Builders<BsonDocument>.Filter.Eq("id", characterId.ToString())
        );
        
        var result = await collection.Find(filter).FirstOrDefaultAsync();
        
        result.Should().NotBeNull();
        result!["Name"].AsString.Should().Be("TestHero");
        result["Level"].AsInt32.Should().Be(10);
        result["Health"].AsInt32.Should().Be(100);

        // Cleanup
        await collection.DeleteOneAsync(filter);
    }

    [Fact]
    public async Task ShouldProcessCharacterUpdatedMessage()
    {
        // Arrange
        var characterId = Guid.NewGuid();
        var collection = _mongoDatabase.GetCollection<BsonDocument>("Characters");

        // Insert initial document
        var initialDoc = new BsonDocument
        {
            ["Id"] = BsonValue.Create(characterId.ToString()),
            ["Name"] = BsonValue.Create("OldName"),
            ["Level"] = BsonValue.Create(5)
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

        // Act
        await _rabbitChannel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "character.updated",
            body: body);

        await Task.Delay(3000);

        // Assert
        var filter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Eq("Id", characterId.ToString()),
            Builders<BsonDocument>.Filter.Eq("id", characterId.ToString())
        );
        
        var result = await collection.Find(filter).FirstOrDefaultAsync();
        
        result.Should().NotBeNull();
        result!["Name"].AsString.Should().Be("UpdatedName");
        result["Level"].AsInt32.Should().Be(15);
        result["Health"].AsInt32.Should().Be(200);

        // Cleanup
        await collection.DeleteOneAsync(filter);
    }

    [Fact]
    public async Task ShouldProcessCharacterDeletedMessage()
    {
        // Arrange
        var characterId = Guid.NewGuid();
        var collection = _mongoDatabase.GetCollection<BsonDocument>("Characters");

        // Insert document to delete
        var doc = new BsonDocument
        {
            ["Id"] = BsonValue.Create(characterId.ToString()),
            ["Name"] = BsonValue.Create("ToBeDeleted")
        };
        await collection.InsertOneAsync(doc);

        // Verify it exists
        var initialFilter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Eq("Id", characterId.ToString()),
            Builders<BsonDocument>.Filter.Eq("id", characterId.ToString())
        );
        (await collection.Find(initialFilter).FirstOrDefaultAsync()).Should().NotBeNull();

        // Delete message
        var deleteMessage = new { Id = characterId };
        var message = JsonSerializer.Serialize(deleteMessage);
        var body = Encoding.UTF8.GetBytes(message);

        // Act
        await _rabbitChannel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "character.deleted",
            body: body);

        await Task.Delay(3000);

        // Assert
        var result = await collection.Find(initialFilter).FirstOrDefaultAsync();
        result.Should().BeNull();
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

        // Act
        await _rabbitChannel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "item.created",
            body: body);

        await Task.Delay(3000);

        // Assert
        var collection = _mongoDatabase.GetCollection<BsonDocument>("Items");
        var filter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Eq("Id", itemId.ToString()),
            Builders<BsonDocument>.Filter.Eq("id", itemId.ToString())
        );
        
        var result = await collection.Find(filter).FirstOrDefaultAsync();
        
        result.Should().NotBeNull();
        result!["Name"].AsString.Should().Be("Magic Sword");
        result["Type"].AsString.Should().Be("Weapon");
        result["Damage"].AsInt32.Should().Be(50);
        result["Rarity"].AsString.Should().Be("Legendary");

        // Cleanup
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

        // Act
        await _rabbitChannel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "character.created",
            body: body);

        await Task.Delay(3000);

        // Assert - Check Outbox using BsonDocument to handle Binary payload
        var outboxCollection = _mongoDatabase.GetCollection<BsonDocument>("OutboxMessages");
        var outboxFilter = Builders<BsonDocument>.Filter.Exists("Payload");
        var outboxResult = await outboxCollection.Find(outboxFilter).FirstOrDefaultAsync();

        outboxResult.Should().NotBeNull();
        
        // Cleanup
        var characterCollection = _mongoDatabase.GetCollection<BsonDocument>("Characters");
        await characterCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("Id", characterId.ToString()));
        if (outboxResult != null)
        {
            await outboxCollection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", outboxResult["_id"]));
        }
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

        // Act
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
        var collection = _mongoDatabase.GetCollection<BsonDocument>("Characters");
        
        foreach (var character in characters)
        {
            var filter = Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Eq("Id", character.Id.ToString()),
                Builders<BsonDocument>.Filter.Eq("id", character.Id.ToString())
            );
            
            var result = await collection.Find(filter).FirstOrDefaultAsync();
            
            result.Should().NotBeNull();
            result!["Name"].AsString.Should().Be(character.Name);
            result["Level"].AsInt32.Should().Be(character.Level);
        }

        // Cleanup
        foreach (var character in characters)
        {
            var filter = Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Eq("Id", character.Id.ToString()),
                Builders<BsonDocument>.Filter.Eq("id", character.Id.ToString())
            );
            await collection.DeleteOneAsync(filter);
        }
    }
}
