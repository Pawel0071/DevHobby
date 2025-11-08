using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Quests;
using RPG.Domain.Entities.Skills;
using RPG.Domain.Enums;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Helpers;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.MongoDB;
using RPG.Infrastructure.Repositories.Redis;
using RedisWarmUp.Services;
using StackExchange.Redis;

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
        services.AddSingleton(typeof(Infrastructure.Interfaces.ILogger<>), typeof(LoggerAdapter<>));

    services.AddSingleton<IActivityScope, NoopActivityScope>();

        // Register MongoDB and Redis
        services.AddSingleton(_mongoDatabase);
        services.AddSingleton(_redisDatabase);

        // Register repositories
        services.AddSingleton<IRedisDocumentRepository, RedisDocumentRepository>();
        services.AddSingleton<IMongoDocumentRepository, MongoDocumentRepository>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task MongoDocumentRepository_ShouldReadAllDocuments()
    {
        // Arrange
        var collection = _mongoDatabase.GetCollection<CharacterDocument>(CharacterDocument.CollectionName);
        await collection.DeleteManyAsync(FilterDefinition<CharacterDocument>.Empty);

        var testDocuments = new[]
        {
            new CharacterDocument { Id = Guid.NewGuid(), Name = "Hero1", PlayerId = Guid.NewGuid(), SessionId = Guid.NewGuid(), Class = "Warrior", Level = 10, Experience = 0, ExperienceToNextLevel = 100, CurrentHealth = 100, MaxHealth = 100, CurrentResource = 50, MaxResource = 50 },
            new CharacterDocument { Id = Guid.NewGuid(), Name = "Hero2", PlayerId = Guid.NewGuid(), SessionId = Guid.NewGuid(), Class = "Mage", Level = 20, Experience = 0, ExperienceToNextLevel = 100, CurrentHealth = 100, MaxHealth = 100, CurrentResource = 50, MaxResource = 50 },
            new CharacterDocument { Id = Guid.NewGuid(), Name = "Hero3", PlayerId = Guid.NewGuid(), SessionId = Guid.NewGuid(), Class = "Rogue", Level = 30, Experience = 0, ExperienceToNextLevel = 100, CurrentHealth = 100, MaxHealth = 100, CurrentResource = 50, MaxResource = 50 }
        };
        await collection.InsertManyAsync(testDocuments);

        var repository = _serviceProvider.GetRequiredService<IMongoDocumentRepository>();

        // Act
        var documents = await repository.GetAllAsync<CharacterDocument>();

        // Assert
        documents.Should().HaveCount(3);
        documents.Should().AllSatisfy(doc =>
        {
            doc.Id.Should().NotBeEmpty();
            doc.Name.Should().NotBeNullOrEmpty();
            doc.Level.Should().BePositive();
        });
    }

    [Fact]
    public async Task MongoDocumentRepository_ShouldReadInBatches()
    {
        // Arrange
        var collection = _mongoDatabase.GetCollection<ItemDocument>(ItemDocument.CollectionName);
        await collection.DeleteManyAsync(FilterDefinition<ItemDocument>.Empty);

        var testDocuments = new List<ItemDocument>();
        for (var i = 0; i < 25; i++)
            testDocuments.Add(new ItemDocument
            {
                Id = Guid.NewGuid(), Name = $"Item{i}", TypeCode = "WEAPON", Rarity = ItemRarity.Common, RequiredLevel = 1, StackSize = 1
            });
        await collection.InsertManyAsync(testDocuments);

        var repository = _serviceProvider.GetRequiredService<IMongoDocumentRepository>();

        // Act
        var batch1 = await repository.GetBatchAsync<ItemDocument>(0, 10); // skip=0, limit=10
        var batch2 = await repository.GetBatchAsync<ItemDocument>(10, 10); // skip=10, limit=10
        var batch3 = await repository.GetBatchAsync<ItemDocument>(20, 10); // skip=20, limit=10

        // Assert
        batch1.Should().HaveCount(10);
        batch2.Should().HaveCount(10);
        batch3.Should().HaveCount(5);
    }

    [Fact]
    public async Task MongoDocumentRepository_ShouldGetCorrectCount()
    {
        // Arrange
        var collection = _mongoDatabase.GetCollection<SkillDocument>(SkillDocument.CollectionName);
        await collection.DeleteManyAsync(FilterDefinition<SkillDocument>.Empty);

        var testDocuments = new[]
        {
            new SkillDocument { Id = Guid.NewGuid(), Name = "Fireball" },
            new SkillDocument { Id = Guid.NewGuid(), Name = "Heal" },
            new SkillDocument { Id = Guid.NewGuid(), Name = "Shield" },
            new SkillDocument { Id = Guid.NewGuid(), Name = "Teleport" }
        };
        await collection.InsertManyAsync(testDocuments);

        var repository = _serviceProvider.GetRequiredService<IMongoDocumentRepository>();

        // Act
        var count = await repository.CountAsync<SkillDocument>();

        // Assert
        count.Should().Be(4);
    }

    [Fact]
    public async Task RedisDocumentWriter_ShouldWriteSingleDocument()
    {
        // Arrange
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentRepository>();
        var document = new CharacterDocument { Id = Guid.NewGuid(), Name = "TestCharacter", PlayerId = Guid.NewGuid(), SessionId = Guid.NewGuid(), Class = "Warrior", Level = 42, Experience = 0, ExperienceToNextLevel = 100, CurrentHealth = 100, MaxHealth = 100, CurrentResource = 50, MaxResource = 50 };

        // Act
        await writer.UpsertAsync(document);

        // Assert
        var redisKey = $"{CharacterDocument.CollectionName}:{document.Id}";
        var exists = await _redisDatabase.KeyExistsAsync(redisKey);
        exists.Should().BeTrue();

        var value = await _redisDatabase.StringGetAsync(redisKey);
        value.HasValue.Should().BeTrue();

        var stored = JsonSerializer.Deserialize<CharacterDocument>(value!);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("TestCharacter");
    }

    [Fact]
    public async Task RedisDocumentWriter_ShouldWriteBatchDocuments()
    {
        // Arrange
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentRepository>();
        var documents = new List<ItemDocument>();

        for (var i = 0; i < 5; i++)
        {
            documents.Add(new ItemDocument
            {
                Id = Guid.NewGuid(),
                Name = $"Item{i}",
                TypeCode = "POTION",
                Rarity = ItemRarity.Rare,
                RequiredLevel = 1,
                StackSize = 10
            });
        }

        // Act
        foreach(var doc in documents)
        {
            await writer.UpsertAsync(doc);
        }

        // Assert
        foreach (var doc in documents)
        {
            var key = $"{ItemDocument.CollectionName}:{doc.Id}";
            var exists = await _redisDatabase.KeyExistsAsync(key);
            exists.Should().BeTrue($"Key {key} should exist in Redis");
        }
    }

    [Fact]
    public async Task RedisDocumentWriter_ShouldSetExpiryCorrectly()
    {
        // Arrange
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentRepository>();
        var document = new SkillDocument { Id = Guid.NewGuid(), Name = "TempData" };
        
        // Act
        await writer.UpsertAsync(document);
        var redisKey = $"{SkillDocument.CollectionName}:{document.Id}";
        await _redisDatabase.KeyExpireAsync(redisKey, TimeSpan.FromSeconds(5));

        // Assert
        var ttl = await _redisDatabase.KeyTimeToLiveAsync(redisKey);
        ttl.Should().NotBeNull();
        ttl.Value.TotalSeconds.Should().BeInRange(1, 6); // Allow some tolerance
    }

    [Fact]
    public async Task RedisDocumentWriter_ShouldCheckExistence()
    {
        // Arrange
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentRepository>();
        var existingId = Guid.NewGuid();
        var nonExistingId = Guid.NewGuid();

        var document = new QuestDocument { Id = existingId, Title = "Test Quest" };

        await writer.UpsertAsync(document);

        // Act
        var exists = await writer.GetByIdAsync<QuestDocument>(existingId);
        var notExists = await writer.GetByIdAsync<QuestDocument>(nonExistingId);

        // Assert
        exists.Should().NotBeNull();
        notExists.Should().BeNull();
    }

    [Fact]
    public async Task RedisDocumentWriter_ShouldDeleteDocument()
    {
        // Arrange
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentRepository>();
        var documentId = Guid.NewGuid();
        var document = new CharacterDocument { Id = documentId, Name = "ToDelete", PlayerId = Guid.NewGuid(), SessionId = Guid.NewGuid(), Class = "Warrior", Level = 1, Experience = 0, ExperienceToNextLevel = 100, CurrentHealth = 100, MaxHealth = 100, CurrentResource = 50, MaxResource = 50 };

        await writer.UpsertAsync(document);
        var existsBefore = await writer.GetByIdAsync<CharacterDocument>(documentId);

        // Act
        await writer.DeleteAsync<CharacterDocument>(documentId);

        // Assert
        existsBefore.Should().NotBeNull();
        var existsAfter = await writer.GetByIdAsync<CharacterDocument>(documentId);
        existsAfter.Should().BeNull();
    }

    [Fact]
    public async Task EndToEnd_MongoToRedis_ShouldTransferDocuments()
    {
        // Arrange
        var collectionName = "Characters";
        var collection = _mongoDatabase.GetCollection<CharacterDocument>(collectionName);
        await collection.DeleteManyAsync(FilterDefinition<CharacterDocument>.Empty);

        var testDocuments = new[]
        {
            new CharacterDocument { Id = Guid.NewGuid(), Name = "Warrior", PlayerId = Guid.NewGuid(), SessionId = Guid.NewGuid(), Class = "Warrior", Level = 50, Experience = 0, ExperienceToNextLevel = 100, CurrentHealth = 100, MaxHealth = 100, CurrentResource = 50, MaxResource = 50 },
            new CharacterDocument { Id = Guid.NewGuid(), Name = "Mage", PlayerId = Guid.NewGuid(), SessionId = Guid.NewGuid(), Class = "Mage", Level = 45, Experience = 0, ExperienceToNextLevel = 100, CurrentHealth = 100, MaxHealth = 100, CurrentResource = 50, MaxResource = 50 },
            new CharacterDocument { Id = Guid.NewGuid(), Name = "Rogue", PlayerId = Guid.NewGuid(), SessionId = Guid.NewGuid(), Class = "Rogue", Level = 48, Experience = 0, ExperienceToNextLevel = 100, CurrentHealth = 100, MaxHealth = 100, CurrentResource = 50, MaxResource = 50 }
        };
        await collection.InsertManyAsync(testDocuments);

        var repository = _serviceProvider.GetRequiredService<IMongoDocumentRepository>();
        var writer = _serviceProvider.GetRequiredService<IRedisDocumentRepository>();

        // Act - Simulate what RedisWarmUpOrchestrator does
        var documents = await repository.GetAllAsync<CharacterDocument>();
        foreach(var doc in documents)
        {
            await writer.UpsertAsync(doc);
        }

        // Assert
        foreach (var doc in documents)
        {
            var key = $"{collectionName}:{doc.Id}";
            var exists = await _redisDatabase.KeyExistsAsync(key);
            exists.Should().BeTrue($"Key {key} should be cached in Redis");

            var value = await _redisDatabase.StringGetAsync(key);
            value.HasValue.Should().BeTrue();

            var stored = JsonSerializer.Deserialize<CharacterDocument>(value!);
            stored.Should().NotBeNull();
            stored!.Name.Should().Be(doc.Name);
            stored.Level.Should().Be(doc.Level);
        }
    }

    [Fact]
    public async Task RedisWarmUpService_ShouldWarmUpAllMappedDocuments()
    {
        // Arrange
        await FlushRedisAsync();
        await ClearMongoCollectionsAsync();

        var redisRepo = _serviceProvider.GetRequiredService<IRedisDocumentRepository>();
        var mongoRepo = _serviceProvider.GetRequiredService<IMongoDocumentRepository>();
        var logger = _serviceProvider.GetRequiredService<Infrastructure.Interfaces.ILogger<RedisWarmUpService>>();

    var seededDocuments = new List<object>();
        foreach (var mapping in DocumentMappingRegistry.All)
        {
            var document = CreateDocumentInstance(mapping.DocumentType);
            await InsertDocumentAsync(document);
            seededDocuments.Add(document);
        }

        var strategies = DocumentMappingRegistry.All
            .Select(mapping =>
            {
                var strategyType = typeof(DocumentWarmUpStrategy<>).MakeGenericType(mapping.DocumentType);
                return (RedisWarmUp.Services.IDocumentWarmUpStrategy)Activator.CreateInstance(strategyType, mongoRepo, mapping.CollectionName)!;
            })
            .ToList();

        var warmUpService = new RedisWarmUp.Services.RedisWarmUpService(redisRepo, strategies, logger);

        // Act
        await warmUpService.ExecuteAsync(CancellationToken.None);

        // Assert
        foreach (var document in seededDocuments)
        {
            await AssertRedisContainsDocumentAsync(redisRepo, document);
        }
    }

    // Helper class for logger adapter
    public class LoggerAdapter<T> : Infrastructure.Interfaces.ILogger<T>
    {
        private readonly Microsoft.Extensions.Logging.ILogger<T> _logger;

        public LoggerAdapter(Microsoft.Extensions.Logging.ILogger<T> logger)
        {
            _logger = logger;
        }

        public void Info(string message)
        {
            _logger.LogInformation(message);
        }

        public void Warn(string message)
        {
            _logger.LogWarning(message);
        }

        public void Error(string message, Exception? exception = null)
        {
            _logger.LogError(exception, message);
        }

        public void Debug(string message)
        {
            _logger.LogDebug(message);
        }
    }

    private sealed class NoopActivityScope : IActivityScope
    {
        public IDisposable? Start(string name, IDictionary<string, object>? tags = null) => null;
    }

    private async Task FlushRedisAsync()
    {
        var endpoints = _fixture.RedisConnection.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _fixture.RedisConnection.GetServer(endpoint);
            await server.FlushDatabaseAsync();
        }
    }

    private async Task ClearMongoCollectionsAsync()
    {
        foreach (var mapping in DocumentMappingRegistry.All)
        {
            var generic = typeof(RedisWarmUpIntegrationTests)
                .GetMethod(nameof(DeleteAllDocumentsAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(mapping.DocumentType);
            await (Task)generic.Invoke(this, null)!;
        }
    }

    private async Task DeleteAllDocumentsAsync<TDocument>() where TDocument : class, IMongoDocument
    {
        var collection = _mongoDatabase.GetCollection<TDocument>(TDocument.CollectionName);
        await collection.DeleteManyAsync(FilterDefinition<TDocument>.Empty);
    }

    private async Task InsertDocumentAsync(object document)
    {
        var genericMethod = typeof(RedisWarmUpIntegrationTests)
            .GetMethod(nameof(InsertTypedDocumentAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var constructed = genericMethod.MakeGenericMethod(document.GetType());
        await (Task)constructed.Invoke(this, new object[] { document })!;
    }

    private async Task InsertTypedDocumentAsync<TDocument>(TDocument document) where TDocument : class, IMongoDocument
    {
        var collection = _mongoDatabase.GetCollection<TDocument>(TDocument.CollectionName);
        await collection.InsertOneAsync(document);
    }

    private async Task AssertRedisContainsDocumentAsync(IRedisDocumentRepository redisRepository, object document)
    {
        var genericMethod = typeof(RedisWarmUpIntegrationTests)
            .GetMethod(nameof(AssertRedisContainsTypedDocumentAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var constructed = genericMethod.MakeGenericMethod(document.GetType());
        await (Task)constructed.Invoke(this, new object[] { redisRepository, document })!;
    }

    private async Task AssertRedisContainsTypedDocumentAsync<TDocument>(IRedisDocumentRepository redisRepository, TDocument document)
        where TDocument : class, IMongoDocument
    {
        var cached = await redisRepository.GetByIdAsync<TDocument>(document.Id);
        cached.Should().NotBeNull($"Document {typeof(TDocument).Name} with Id {document.Id} should be cached in Redis");
    }

    private static object CreateDocumentInstance(Type documentType)
    {
        if (documentType == typeof(CharacterDocument))
        {
            return new CharacterDocument
            {
                Id = Guid.NewGuid(),
                Name = "WarmUpCharacter",
                PlayerId = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
                Class = "Warrior",
                Level = 5,
                Experience = 100,
                ExperienceToNextLevel = 200,
                CurrentHealth = 100,
                MaxHealth = 120,
                CurrentResource = 40,
                MaxResource = 80,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        if (documentType == typeof(ItemDocument))
        {
            return new ItemDocument
            {
                Id = Guid.NewGuid(),
                Name = "WarmUpItem",
                TypeCode = "TEST",
                Rarity = ItemRarity.Common,
                RequiredLevel = 1,
                StackSize = 1
            };
        }

        if (documentType == typeof(SkillDocument))
        {
            return new SkillDocument
            {
                Id = Guid.NewGuid(),
                Name = "WarmUpSkill",
                Description = "Test skill"
            };
        }

        if (documentType == typeof(QuestDocument))
        {
            return new QuestDocument
            {
                Id = Guid.NewGuid(),
                Title = "WarmUpQuest",
                Description = "Test quest",
                StartLocation = new LocationData { X = 1, Y = 2, Z = 3 }
            };
        }

        if (documentType == typeof(NpcDocument))
        {
            return new NpcDocument
            {
                Id = Guid.NewGuid(),
                Name = "WarmUpNpc",
                Level = 3,
                CurrentHealth = 80,
                MaxHealth = 80,
                SpawnLocation = new LocationData { X = 5, Y = 1, Z = 0 },
                WorldId = Guid.NewGuid()
            };
        }

        if (documentType == typeof(PlayerDocument))
        {
            return new PlayerDocument
            {
                Id = Guid.NewGuid(),
                Username = "warmup-player",
                Email = "warmup@test.dev",
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                IsOnline = true
            };
        }

        if (documentType == typeof(MapObjectDocument))
        {
            return new MapObjectDocument
            {
                Id = Guid.NewGuid(),
                Name = "WarmUpObject",
                DisplayName = "Object",
                Description = "Test object",
                Location = new LocationData { X = 10, Y = 0, Z = -5 },
                RotationYaw = 45,
                WorldId = Guid.NewGuid(),
                ZoneId = "test-zone"
            };
        }

        if (documentType == typeof(WorldStateDocument))
        {
            return new WorldStateDocument
            {
                Id = Guid.NewGuid(),
                WorldId = Guid.NewGuid(),
                WorldName = "WarmUpWorld",
                LastUpdated = DateTime.UtcNow
            };
        }

        throw new InvalidOperationException($"Unknown document type {documentType.Name}");
    }
}
