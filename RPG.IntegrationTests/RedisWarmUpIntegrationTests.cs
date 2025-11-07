using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Configuration;
using RPG.Infrastructure.Repositories.Redis;
using RPG.Infrastructure.Repositories.MongoDB;
using RPG.Infrastructure.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace RPG.IntegrationTests;

public class RedisWarmUpIntegrationTests : IClassFixture<TestContainersFixture>
{
    private readonly TestContainersFixture _fixture;
    private readonly IMongoDatabase _mongoDatabase;
    private readonly IDatabase _redisDatabase;
    private readonly ServiceProvider _serviceProvider;

    public RedisWarmUpIntegrationTests(TestContainersFixture fixture)
    {
        _fixture = fixture;
        _mongoDatabase = _fixture.MongoDatabase;
        _redisDatabase = _fixture.RedisDatabase;

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Register Infrastructure logger adapter
        services.AddSingleton(typeof(RPG.Infrastructure.Interfaces.ILogger<>), typeof(LoggerAdapter<>));
        
        // Register MongoDB and Redis
        services.AddSingleton(_mongoDatabase);
        services.AddSingleton(_redisDatabase);
        
        // Register repositories and services
        services.AddSingleton<IMongoDocumentReader, MongoDocumentReader>();
        services.AddSingleton<IRedisDocumentWriter, RedisDocumentWriter>();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task MongoDocumentReader_ShouldReadAllDocuments()
    {
        // Arrange
        var collection = _mongoDatabase.GetCollection<BsonDocument>("Characters");
        await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);

        var testDocuments = new[]
        {
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Hero1" }, { "Level", 10 } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Hero2" }, { "Level", 20 } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Hero3" }, { "Level", 30 } }
        };
        await collection.InsertManyAsync(testDocuments);

        var reader = _serviceProvider.GetRequiredService<IMongoDocumentReader>();

        // Act
        var documents = await reader.ReadAllAsync("Characters");

        // Assert
        documents.Should().HaveCount(3);
        documents.Should().AllSatisfy(doc =>
        {
            doc.Should().ContainKey("Id");
            doc.Should().ContainKey("Name");
            doc.Should().ContainKey("Level");
        });
    }

    [Fact]
    public async Task MongoDocumentReader_ShouldReadInBatches()
    {
        // Arrange
        var collection = _mongoDatabase.GetCollection<BsonDocument>("Items");
        await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);

        var testDocuments = new List<BsonDocument>();
        for (int i = 0; i < 25; i++)
        {
            testDocuments.Add(new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "Name", $"Item{i}" },
                { "Rarity", i % 5 }
            });
        }
        await collection.InsertManyAsync(testDocuments);

        var reader = _serviceProvider.GetRequiredService<IMongoDocumentReader>();

        // Act
        var batch1 = await reader.ReadBatchAsync("Items", 10, 0);
        var batch2 = await reader.ReadBatchAsync("Items", 10, 10);
        var batch3 = await reader.ReadBatchAsync("Items", 10, 20);

        // Assert
        batch1.Should().HaveCount(10);
        batch2.Should().HaveCount(10);
        batch3.Should().HaveCount(5);
    }

    [Fact]
    public async Task MongoDocumentReader_ShouldGetCorrectCount()
    {
        // Arrange
        var collection = _mongoDatabase.GetCollection<BsonDocument>("Skills");
        await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);

        var testDocuments = new[]
        {
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Fireball" } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Heal" } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Shield" } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Teleport" } }
        };
        await collection.InsertManyAsync(testDocuments);

        var reader = _serviceProvider.GetRequiredService<IMongoDocumentReader>();

        // Act
        var count = await reader.GetCountAsync("Skills");

        // Assert
        count.Should().Be(4);
    }

    [Fact]
    public async Task RedisDocumentWriter_ShouldWriteSingleDocument()
    {
        // Arrange
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentWriter>();
        var documentId = Guid.NewGuid();
        var document = new Dictionary<string, JsonElement>
        {
            { "Id", JsonSerializer.SerializeToElement(documentId.ToString()) },
            { "Name", JsonSerializer.SerializeToElement("TestCharacter") },
            { "Level", JsonSerializer.SerializeToElement(42) }
        };
        var redisKey = $"Characters:{documentId}";
        var documentJson = JsonSerializer.Serialize(document);

        // Act
        await writer.WriteAsync(redisKey, documentJson, TimeSpan.FromMinutes(5));

        // Assert
        var exists = await _redisDatabase.KeyExistsAsync(redisKey);
        exists.Should().BeTrue();

        var value = await _redisDatabase.StringGetAsync(redisKey);
        value.HasValue.Should().BeTrue();
        
        var stored = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value!);
        stored.Should().ContainKey("Name");
        stored!["Name"].GetString().Should().Be("TestCharacter");
    }

    [Fact]
    public async Task RedisDocumentWriter_ShouldWriteBatchDocuments()
    {
        // Arrange
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentWriter>();
        var keyValuePairs = new Dictionary<string, string>();
        
        for (int i = 0; i < 5; i++)
        {
            var id = Guid.NewGuid();
            var document = new Dictionary<string, JsonElement>
            {
                { "Id", JsonSerializer.SerializeToElement(id.ToString()) },
                { "Name", JsonSerializer.SerializeToElement($"Item{i}") },
                { "Price", JsonSerializer.SerializeToElement(100 * i) }
            };
            var key = $"Items:{id}";
            keyValuePairs[key] = JsonSerializer.Serialize(document);
        }

        // Act
        await writer.WriteBatchAsync(keyValuePairs, TimeSpan.FromMinutes(10));

        // Assert
        foreach (var kvp in keyValuePairs)
        {
            var exists = await _redisDatabase.KeyExistsAsync(kvp.Key);
            exists.Should().BeTrue($"Key {kvp.Key} should exist in Redis");
        }
    }

    [Fact]
    public async Task RedisDocumentWriter_ShouldSetExpiryCorrectly()
    {
        // Arrange
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentWriter>();
        var documentId = Guid.NewGuid();
        var document = new Dictionary<string, JsonElement>
        {
            { "Id", JsonSerializer.SerializeToElement(documentId.ToString()) },
            { "Name", JsonSerializer.SerializeToElement("TempData") }
        };
        var redisKey = $"TempCollection:{documentId}";
        var documentJson = JsonSerializer.Serialize(document);
        var expiry = TimeSpan.FromSeconds(5);

        // Act
        await writer.WriteAsync(redisKey, documentJson, expiry);

        // Assert
        var ttl = await _redisDatabase.KeyTimeToLiveAsync(redisKey);
        ttl.Should().NotBeNull();
        ttl.Value.TotalSeconds.Should().BeInRange(1, 6); // Allow some tolerance
    }

    [Fact]
    public async Task RedisDocumentWriter_ShouldCheckExistence()
    {
        // Arrange
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentWriter>();
        var existingId = Guid.NewGuid();
        var nonExistingId = Guid.NewGuid();
        
        var document = new Dictionary<string, JsonElement>
        {
            { "Id", JsonSerializer.SerializeToElement(existingId.ToString()) }
        };
        var redisKey = $"Quests:{existingId}";
        var documentJson = JsonSerializer.Serialize(document);
        
        await writer.WriteAsync(redisKey, documentJson, TimeSpan.FromMinutes(1));

        // Act
        var exists = await writer.ExistsAsync(redisKey);
        var notExists = await writer.ExistsAsync($"Quests:{nonExistingId}");

        // Assert
        exists.Should().BeTrue();
        notExists.Should().BeFalse();
    }

    [Fact]
    public async Task RedisDocumentWriter_ShouldDeleteDocument()
    {
        // Arrange
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentWriter>();
        var documentId = Guid.NewGuid();
        var document = new Dictionary<string, JsonElement>
        {
            { "Id", JsonSerializer.SerializeToElement(documentId.ToString()) }
        };
        var redisKey = $"Worlds:{documentId}";
        var documentJson = JsonSerializer.Serialize(document);
        
        await writer.WriteAsync(redisKey, documentJson, TimeSpan.FromMinutes(1));
        var existsBefore = await writer.ExistsAsync(redisKey);

        // Act
        await writer.DeleteAsync(redisKey);

        // Assert
        existsBefore.Should().BeTrue();
        var existsAfter = await writer.ExistsAsync(redisKey);
        existsAfter.Should().BeFalse();
    }

    [Fact]
    public async Task EndToEnd_MongoToRedis_ShouldTransferDocuments()
    {
        // Arrange
        var collectionName = "Characters";
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);

        var testDocuments = new[]
        {
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Warrior" }, { "Level", 50 } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Mage" }, { "Level", 45 } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Name", "Rogue" }, { "Level", 48 } }
        };
        await collection.InsertManyAsync(testDocuments);

        var reader = _serviceProvider.GetRequiredService<IMongoDocumentReader>();
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentWriter>();

        // Act - Simulate what RedisWarmUpService does
        var documents = await reader.ReadAllAsync(collectionName);
        var keyValuePairs = new Dictionary<string, string>();
        
        foreach (var doc in documents)
        {
            var id = Guid.Parse(doc["Id"].GetString()!);
            var key = $"{collectionName}:{id}";
            keyValuePairs[key] = JsonSerializer.Serialize(doc);
        }
        
        await writer.WriteBatchAsync(keyValuePairs, TimeSpan.FromMinutes(30));

        // Assert
        foreach (var kvp in keyValuePairs)
        {
            var exists = await writer.ExistsAsync(kvp.Key);
            exists.Should().BeTrue($"Key {kvp.Key} should be cached in Redis");

            var value = await _redisDatabase.StringGetAsync(kvp.Key);
            value.HasValue.Should().BeTrue();
            
            var stored = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value!);
            stored.Should().ContainKey("Name");
            stored.Should().ContainKey("Level");
        }
    }

    [Fact]
    public async Task RedisWarmUpService_ShouldWarmUpMultipleCollections()
    {
        // Arrange
        var settings = new RedisWarmUpSettings
        {
            CollectionsToCache = new List<string> { "Characters", "Items", "Skills" },
            BatchSize = 10,
            IntervalSeconds = 1,
            CacheExpirySeconds = 300
        };

        // Seed MongoDB with test data
        foreach (var collectionName in settings.CollectionsToCache)
        {
            var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
            await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);

            var testDoc = new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "Name", $"Test{collectionName}" },
                { "Value", 123 }
            };
            await collection.InsertOneAsync(testDoc);
        }

        var reader = _serviceProvider.GetRequiredService<IMongoDocumentReader>();
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentWriter>();
        var logger = _serviceProvider.GetRequiredService<RPG.Infrastructure.Interfaces.ILogger<RedisWarmUpService>>();
        var warmUpService = new RedisWarmUpService(reader, writer, logger, settings);

        // Act
        await warmUpService.WarmUpCycleAsync(CancellationToken.None);

        // Assert
        foreach (var collectionName in settings.CollectionsToCache)
        {
            var documents = await reader.ReadAllAsync(collectionName);
            documents.Should().NotBeEmpty($"{collectionName} should have documents");

            foreach (var doc in documents)
            {
                var id = Guid.Parse(doc["Id"].GetString()!);
                var key = $"{collectionName}:{id}";
                var exists = await writer.ExistsAsync(key);
                exists.Should().BeTrue($"Document from {collectionName} should be cached at key {key}");
            }
        }
    }

    // Helper class for logger adapter
    public class LoggerAdapter<T> : RPG.Infrastructure.Interfaces.ILogger<T>
    {
        private readonly Microsoft.Extensions.Logging.ILogger<T> _logger;

        public LoggerAdapter(Microsoft.Extensions.Logging.ILogger<T> logger)
        {
            _logger = logger;
        }

        public void Info(string message) => _logger.LogInformation(message);
        public void Warn(string message) => _logger.LogWarning(message);
        public void Error(string message, Exception? exception = null) => 
            _logger.LogError(exception, message);
        public void Debug(string message) => _logger.LogDebug(message);
    }
}
