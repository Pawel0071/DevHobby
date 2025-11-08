using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RedisWarmUp.Services;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using Xunit;

namespace RPG.UnitTest.RedisWarmUp.Services;

public class DocumentWarmUpStrategyTests
{
    private readonly Mock<IMongoDocumentRepository> _mongoRepositoryMock;
    private readonly DocumentWarmUpStrategy<TestDocument> _strategy;

    public DocumentWarmUpStrategyTests()
    {
        _mongoRepositoryMock = new Mock<IMongoDocumentRepository>();
        _strategy = new DocumentWarmUpStrategy<TestDocument>(_mongoRepositoryMock.Object, "TestDocuments");
    }

    private static Mock<IRedisDocumentRepository> CreateRedisRepositoryMock() => new();

    private static Mock<ILogger<RedisWarmUpService>> CreateLoggerMock() => new();

    private class TestDocument : IMongoDocument
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public static string CollectionName => "TestDocuments";
    }

    [Fact]
    public async Task WarmUpAsync_ShouldReturnZero_WhenMongoHasNoDocuments()
    {
        // Arrange
        _mongoRepositoryMock
            .Setup(repo => repo.GetAllAsync<TestDocument>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TestDocument>());

        var redisRepositoryMock = CreateRedisRepositoryMock();
        var loggerMock = CreateLoggerMock();

        // Act
        var processed = await _strategy.WarmUpAsync(redisRepositoryMock.Object, loggerMock.Object, CancellationToken.None);

        // Assert
        processed.Should().Be(0);
        redisRepositoryMock.Verify(repo => repo.UpsertAsync(It.IsAny<TestDocument>(), It.IsAny<CancellationToken>()), Times.Never);
        _mongoRepositoryMock.Verify(repo => repo.GetAllAsync<TestDocument>(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WarmUpAsync_ShouldWriteEveryDocumentToRedis()
    {
        // Arrange
        var documents = new List<TestDocument>
        {
            new() { Id = Guid.NewGuid(), Name = "First" },
            new() { Id = Guid.NewGuid(), Name = "Second" }
        };

        _mongoRepositoryMock
            .Setup(repo => repo.GetAllAsync<TestDocument>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        var redisRepositoryMock = CreateRedisRepositoryMock();
        var loggerMock = CreateLoggerMock();

        // Act
        var processed = await _strategy.WarmUpAsync(redisRepositoryMock.Object, loggerMock.Object, CancellationToken.None);

        // Assert
        processed.Should().Be(documents.Count);
        foreach (var document in documents)
        {
            redisRepositoryMock.Verify(repo => repo.UpsertAsync(document, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task WarmUpAsync_ShouldStopWhenCancellationIsRequested()
    {
        // Arrange
        var first = new TestDocument { Id = Guid.NewGuid(), Name = "First" };
        var second = new TestDocument { Id = Guid.NewGuid(), Name = "Second" };
        var documents = new List<TestDocument> { first, second };

        _mongoRepositoryMock
            .Setup(repo => repo.GetAllAsync<TestDocument>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        var redisRepositoryMock = CreateRedisRepositoryMock();
        var loggerMock = CreateLoggerMock();
        var cts = new CancellationTokenSource();

        redisRepositoryMock
            .Setup(repo => repo.UpsertAsync(first, It.IsAny<CancellationToken>()))
            .Returns<TestDocument, CancellationToken>((_, _) =>
            {
                cts.Cancel();
                return Task.CompletedTask;
            });

        // Act
        var processed = await _strategy.WarmUpAsync(redisRepositoryMock.Object, loggerMock.Object, cts.Token);

        // Assert
        processed.Should().Be(1);
        redisRepositoryMock.Verify(repo => repo.UpsertAsync(first, It.IsAny<CancellationToken>()), Times.Once);
        redisRepositoryMock.Verify(repo => repo.UpsertAsync(second, It.IsAny<CancellationToken>()), Times.Never);
    }
}
