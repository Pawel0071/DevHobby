using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MongoDB.Driver;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.MongoDB;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Repositories.MongoDB;

public class MongoDocumentRepositoryTests
{
    private readonly Mock<IMongoDatabase> _databaseMock = new();
    private readonly Mock<ILogger<MongoDocumentRepository>> _loggerMock = new();

    private MongoDocumentRepository CreateRepository() => new(_databaseMock.Object, _loggerMock.Object);

    private static Mock<IMongoCollection<TestDocument>> CreateCollectionMock() => new();

    private static void SetupCollectionCursor(Mock<IMongoCollection<TestDocument>> collectionMock, IEnumerable<TestDocument> documents)
    {
        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateCursor(documents));

        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateCursor(documents));

        collectionMock
            .Setup(c => c.FindSync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => CreateCursor(documents));

        collectionMock
            .Setup(c => c.FindSync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => CreateCursor(documents));
    }

    private static IAsyncCursor<TestDocument> CreateCursor(IEnumerable<TestDocument> documents) =>
        new InMemoryAsyncCursor<TestDocument>(documents ?? Enumerable.Empty<TestDocument>());

    private sealed class InMemoryAsyncCursor<T> : IAsyncCursor<T>
    {
        private readonly IEnumerator<T> _enumerator;

        public InMemoryAsyncCursor(IEnumerable<T> items)
        {
            _enumerator = (items ?? Enumerable.Empty<T>()).GetEnumerator();
        }

        public IEnumerable<T> Current { get; private set; } = Enumerable.Empty<T>();

        public bool MoveNext(CancellationToken cancellationToken = default)
        {
            if (_enumerator.MoveNext())
            {
                Current = new[] { _enumerator.Current };
                return true;
            }

            Current = Enumerable.Empty<T>();
            return false;
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(MoveNext(cancellationToken));

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }

    public class TestDocument : IMongoDocument
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public static string CollectionName => "TestDocuments";
    }

    [Fact]
    public async Task UpsertAsync_ShouldReplaceExistingDocument()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();
        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        collectionMock
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<TestDocument>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ReplaceOneResult>(r => r.IsAcknowledged));

        var repository = CreateRepository();
        var document = new TestDocument { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        await repository.UpsertAsync(document, CancellationToken.None);

        // Assert
        collectionMock.Verify(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<TestDocument>>(),
            document,
            It.Is<ReplaceOptions>(o => o.IsUpsert),
            It.IsAny<CancellationToken>()), Times.Once);

        _loggerMock.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("upserted successfully"))), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_ShouldLogAndRethrow_WhenMongoThrows()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();
        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var exception = new InvalidOperationException("mongo down");

        collectionMock
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<TestDocument>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var repository = CreateRepository();
        var document = new TestDocument { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        var act = async () => await repository.UpsertAsync(document);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _loggerMock.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to upsert")), exception), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDocument_WhenFound()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();
        var expected = new TestDocument { Id = Guid.NewGuid(), Name = "Found" };

        SetupCollectionCursor(collectionMock, new[] { expected });

        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var repository = CreateRepository();

        // Act
        var result = await repository.GetByIdAsync<TestDocument>(expected.Id);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _loggerMock.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("found"))), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenMissing()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();
        SetupCollectionCursor(collectionMock, Array.Empty<TestDocument>());

        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var repository = CreateRepository();

        // Act
        var result = await repository.GetByIdAsync<TestDocument>(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
        _loggerMock.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("not found"))), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLogAndRethrow_WhenMongoThrows()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();
        var exception = new InvalidOperationException("find failed");

        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        collectionMock
            .Setup(c => c.FindSync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .Throws(exception);

        collectionMock
            .Setup(c => c.FindSync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .Throws(exception);

        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var repository = CreateRepository();

        // Act
        var act = async () => await repository.GetByIdAsync<TestDocument>(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _loggerMock.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to get")), exception), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnDocuments()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();
        var documents = new List<TestDocument>
        {
            new() { Id = Guid.NewGuid(), Name = "One" },
            new() { Id = Guid.NewGuid(), Name = "Two" }
        };

        SetupCollectionCursor(collectionMock, documents);

        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var repository = CreateRepository();

        // Act
        var result = await repository.GetAllAsync<TestDocument>();

        // Assert
        result.Should().HaveCount(2);
        _loggerMock.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("Read 2"))), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLogAndRethrow_WhenMongoThrows()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();
        var exception = new InvalidOperationException("bad cursor");

        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        collectionMock
            .Setup(c => c.FindSync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .Throws(exception);

        collectionMock
            .Setup(c => c.FindSync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .Throws(exception);

        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var repository = CreateRepository();

        // Act
        var act = async () => await repository.GetAllAsync<TestDocument>();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _loggerMock.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to get all")), exception), Times.Once);
    }

    [Fact]
    public async Task GetBatchAsync_ShouldApplySkipAndLimit()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();
        var documents = new List<TestDocument>
        {
            new() { Id = Guid.NewGuid(), Name = "A" }
        };

        SetupCollectionCursor(collectionMock, documents);

        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var repository = CreateRepository();

        // Act
        var result = await repository.GetBatchAsync<TestDocument>(5, 10);

        // Assert
    result.Should().HaveCount(1);
    _loggerMock.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("skip=5") && msg.Contains("limit=10"))), Times.Once);
    }

    [Fact]
    public async Task GetBatchAsync_ShouldLogAndRethrow_WhenMongoThrows()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();
        var exception = new InvalidOperationException("batch error");

        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        collectionMock
            .Setup(c => c.FindSync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .Throws(exception);

        collectionMock
            .Setup(c => c.FindSync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<FindOptions<TestDocument, TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .Throws(exception);

        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var repository = CreateRepository();

        // Act
        var act = async () => await repository.GetBatchAsync<TestDocument>(0, 10);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _loggerMock.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to get batch")), exception), Times.Once);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnNumberOfDocuments()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();
        const long expected = 42;

        collectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var repository = CreateRepository();

        // Act
        var result = await repository.CountAsync<TestDocument>();

        // Assert
        result.Should().Be(expected);
        _loggerMock.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("has 42"))), Times.Once);
    }

    [Fact]
    public async Task CountAsync_ShouldLogAndRethrow_WhenMongoThrows()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();
        var exception = new InvalidOperationException("count failed");

        collectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var repository = CreateRepository();

        // Act
        var act = async () => await repository.CountAsync<TestDocument>();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _loggerMock.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to count")), exception), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenDocumentDeleted()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();

        collectionMock
            .Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<DeleteResult>(r => r.DeletedCount == 1 && r.IsAcknowledged));

        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var repository = CreateRepository();

        // Act
        var result = await repository.DeleteAsync<TestDocument>(Guid.NewGuid());

        // Assert
        result.Should().BeTrue();
        _loggerMock.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("deleted"))), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenDocumentMissing()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();

        collectionMock
            .Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<DeleteResult>(r => r.DeletedCount == 0 && r.IsAcknowledged));

        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var repository = CreateRepository();

        // Act
        var result = await repository.DeleteAsync<TestDocument>(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
        _loggerMock.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("not found"))), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldLogAndRethrow_WhenMongoThrows()
    {
        // Arrange
        var collectionMock = CreateCollectionMock();
        var exception = new InvalidOperationException("delete failed");

        collectionMock
            .Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<TestDocument>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        _databaseMock.Setup(db => db.GetCollection<TestDocument>(TestDocument.CollectionName, null))
            .Returns(collectionMock.Object);

        var repository = CreateRepository();

        // Act
        var act = async () => await repository.DeleteAsync<TestDocument>(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _loggerMock.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to delete")), exception), Times.Once);
    }
}
