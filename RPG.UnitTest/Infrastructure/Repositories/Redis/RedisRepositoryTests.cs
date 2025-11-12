using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.Redis;
using StackExchange.Redis;
using Xunit;

namespace RPG.UnitTest.Infrastructure;

public class RedisRepositoryTests
{
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<ILogger<RedisRepository>> _mockLogger;
    private readonly RedisRepository _repository;
    private readonly Mock<IServer> _mockServer;
    private readonly Mock<IConnectionMultiplexer> _mockConnectionMultiplexer;
    private readonly Mock<IActivityScope> _activityScopeMock = new();
    private readonly IDisposable _activityHandle = Mock.Of<IDisposable>();

    public RedisRepositoryTests()
    {
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<RedisRepository>>();
        _mockServer = new Mock<IServer>();
        _mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();

        _mockConnectionMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
        _mockConnectionMultiplexer.Setup(m => m.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object>())).Returns(_mockServer.Object);
        _mockConnectionMultiplexer.Setup(m => m.GetEndPoints(It.IsAny<bool>())).Returns(new System.Net.EndPoint[] { new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6379) });
        _mockDatabase.Setup(db => db.Multiplexer).Returns(_mockConnectionMultiplexer.Object);

        _activityScopeMock
            .Setup(scope => scope.Start(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
            .Returns(_activityHandle);

        _repository = new RedisRepository(_mockDatabase.Object, _mockLogger.Object, _activityScopeMock.Object);
    }

    private class TestDocument : IPersistenceModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public static string CollectionName => "test_documents";
    }

    [Fact]
    public async Task GetByIdAsync_ShouldDeserialize_WhenDocumentExists()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var testDoc = new TestDocument { Id = docId, Name = "Another User" };
        var json = JsonSerializer.Serialize(testDoc);
        var expectedKey = $"{TestDocument.CollectionName}:{docId}";

        _mockDatabase.Setup(db => db.StringGetAsync(expectedKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(json));

        // Act
        var result = await _repository.GetByIdAsync<TestDocument>(docId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(docId);
        result.Name.Should().Be("Another User");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenDocumentDoesNotExist()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var expectedKey = $"test_documents:{docId}";
        _mockDatabase.Setup(db => db.StringGetAsync(expectedKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _repository.GetByIdAsync<TestDocument>(docId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_ShouldSerializeAndStore()
    {
        // Arrange
        var document = new TestDocument { Id = Guid.NewGuid(), Name = "Test User" };
        var expectedKey = $"{TestDocument.CollectionName}:{document.Id}";

        _mockDatabase.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), false, When.Always, CommandFlags.None))
            .ReturnsAsync(true);

        // Act
        await _repository.UpsertAsync(document);

        // Assert
        _mockDatabase.Verify(db => db.StringSetAsync(
            It.Is<RedisKey>(k => k == expectedKey),
            It.Is<RedisValue>(v => v.ToString().Contains("Test User")),
            null,
            false,
            When.Always,
            CommandFlags.None), Times.Once);
    }
    
    [Fact]
    public async Task GetAllAsync_ShouldReturnMultipleValues()
    {
        // Arrange
        var keys = new RedisKey[] { "test_documents:key1", "test_documents:key2" };
        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), "test_documents:*", It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);

        var doc1 = new TestDocument { Id = Guid.NewGuid(), Name = "Doc1" };
        var doc2 = new TestDocument { Id = Guid.NewGuid(), Name = "Doc2" };
        var values = new RedisValue[] { JsonSerializer.Serialize(doc1), JsonSerializer.Serialize(doc2) };

        _mockDatabase.Setup(db => db.StringGetAsync(keys, It.IsAny<CommandFlags>()))
            .ReturnsAsync(values);

        // Act
        var result = await _repository.GetAllAsync<TestDocument>();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Name == "Doc1");
        result.Should().Contain(d => d.Name == "Doc2");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmpty_WhenNoKeys()
    {
        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), "test_documents:*", It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(Array.Empty<RedisKey>());

        var result = await _repository.GetAllAsync<TestDocument>();

        result.Should().BeEmpty();
        _mockLogger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("No TestDocument"))), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldIgnoreInvalidJson()
    {
        var keys = new RedisKey[] { "test_documents:bad" };
        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), "test_documents:*", It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);

        _mockDatabase.Setup(db => db.StringGetAsync(keys, It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue[] { "not-json" });

        var result = await _repository.GetAllAsync<TestDocument>();

        result.Should().BeEmpty();
        _mockLogger.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("Failed to deserialize"))), Times.Once);
    }

    [Fact]
    public async Task GetBatchAsync_ShouldReturnRequestedRange()
    {
        var keys = new RedisKey[] { "test_documents:1", "test_documents:2", "test_documents:3" };
        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), "test_documents:*", It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);

        var documents = keys.Select((_, index) => new TestDocument { Id = Guid.NewGuid(), Name = $"Doc{index}" }).ToArray();
        var values = documents.Select(d => (RedisValue)JsonSerializer.Serialize(d)).ToArray();

        _mockDatabase.Setup(db => db.StringGetAsync(keys, It.IsAny<CommandFlags>()))
            .ReturnsAsync(values);

        var result = await _repository.GetBatchAsync<TestDocument>(1, 1);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Doc1");
        _mockLogger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("skip=1"))), Times.Once);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnNumberOfKeys()
    {
        var keys = new RedisKey[] { "test_documents:1", "test_documents:2" };
        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), "test_documents:*", It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);

        var count = await _repository.CountAsync<TestDocument>();

        count.Should().Be(2);
        _mockLogger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("has 2"))), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenKeyDeleted()
    {
        var id = Guid.NewGuid();
        var key = (RedisKey)$"test_documents:{id}";

        _mockDatabase.Setup(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var result = await _repository.DeleteAsync<TestDocument>(id);

        result.Should().BeTrue();
        _mockLogger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("deleted"))), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenKeyMissing()
    {
        var id = Guid.NewGuid();
        var key = (RedisKey)$"test_documents:{id}";

        _mockDatabase.Setup(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var result = await _repository.DeleteAsync<TestDocument>(id);

        result.Should().BeFalse();
        _mockLogger.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("not found"))), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldLogAndRethrow_WhenRedisFails()
    {
        var id = Guid.NewGuid();
        var key = (RedisKey)$"test_documents:{id}";
        var exception = new InvalidOperationException("boom");

        _mockDatabase.Setup(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ThrowsAsync(exception);

        var act = async () => await _repository.DeleteAsync<TestDocument>(id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _mockLogger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to delete")), exception), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_ShouldWarn_WhenRedisReturnsFalse()
    {
        var document = new TestDocument { Id = Guid.NewGuid(), Name = "Doc" };

        _mockDatabase.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), false, When.Always, CommandFlags.None))
            .ReturnsAsync(false);

        await _repository.UpsertAsync(document);

        _mockLogger.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("Failed to upsert"))), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_ShouldLogAndRethrow_WhenRedisThrows()
    {
        var document = new TestDocument { Id = Guid.NewGuid(), Name = "Doc" };
        var exception = new InvalidOperationException("write error");

        _mockDatabase.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), false, When.Always, CommandFlags.None))
            .ThrowsAsync(exception);

        var act = async () => await _repository.UpsertAsync(document);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _mockLogger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to upsert")), exception), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLogAndRethrow_WhenRedisThrows()
    {
        var id = Guid.NewGuid();
        var exception = new InvalidOperationException("read error");

        _mockDatabase.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(exception);

        var act = async () => await _repository.GetByIdAsync<TestDocument>(id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _mockLogger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to get")), exception), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLogAndRethrow_WhenServerFails()
    {
        var exception = new InvalidOperationException("server error");

        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), "test_documents:*", It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Throws(exception);

        var act = async () => await _repository.GetAllAsync<TestDocument>();

        await act.Should().ThrowAsync<InvalidOperationException>();
        _mockLogger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to get all")), exception), Times.Once);
    }

    [Fact]
    public async Task GetBatchAsync_ShouldLogAndRethrow_WhenGetAllFails()
    {
        var exception = new InvalidOperationException("inner error");

        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), "test_documents:*", It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Throws(exception);

        var act = async () => await _repository.GetBatchAsync<TestDocument>(0, 2);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _mockLogger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to get batch")), exception), Times.Once);
    }

    [Fact]
    public async Task CountAsync_ShouldLogAndRethrow_WhenServerFails()
    {
        var exception = new InvalidOperationException("count error");

        _mockServer.Setup(s => s.Keys(It.IsAny<int>(), "test_documents:*", It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Throws(exception);

        var act = async () => await _repository.CountAsync<TestDocument>();

        await act.Should().ThrowAsync<InvalidOperationException>();
        _mockLogger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to count")), exception), Times.Once);
    }
}
