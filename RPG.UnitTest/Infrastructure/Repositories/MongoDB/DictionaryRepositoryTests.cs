using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using RPG.Domain.Common;
using RPG.Domain.Enums;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.Orchestrators;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Repositories.MongoDB;

public class DictionaryRepositoryTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase = new();
    private readonly Mock<IMongoCollection<TagDefinition>> _mockCollection = new();
    private readonly Mock<ILogger<DictionaryRepository<TagDefinition>>> _mockLogger = new();
    private readonly DictionaryRepository<TagDefinition> _repository;

    public DictionaryRepositoryTests()
    {
        _mockDatabase
            .Setup(db => db.GetCollection<TagDefinition>(It.IsAny<string>(), null))
            .Returns(_mockCollection.Object);

        _repository = new DictionaryRepository<TagDefinition>(_mockDatabase.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyCollection_ShouldReturnEmptyList()
    {
        SetupFindForGetAll(Array.Empty<TagDefinition>());

        var result = await _repository.GetAllAsync();

        result.Should().BeEmpty();
        _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Loaded 0"))), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleItems_ShouldReturnResults()
    {
        var tags = new[]
        {
            CreateDefinition("item:weapon"),
            CreateDefinition("item:armor")
        };

        SetupFindForGetAll(tags);

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Code == "item:weapon");
        _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Loaded 2"))), Times.Once);
    }

    [Fact]
    public async Task GetByCodeAsync_WithExistingCode_ShouldReturnEntry()
    {
        var expected = CreateDefinition("item:consumable");

        SetupFindForGetByCode(expected);

        var result = await _repository.GetByCodeAsync("item:consumable");

        result.Should().NotBeNull();
        result!.Code.Should().Be("item:consumable");
    }

    [Fact]
    public async Task GetByCodeAsync_WithMissingCode_ShouldReturnNullAndWarn()
    {
        SetupFindForGetByCode(null);

        var result = await _repository.GetByCodeAsync("missing");

        result.Should().BeNull();
        _mockLogger.Verify(l => l.Warn(It.Is<string>(s => s.Contains("not found"))), Times.Once);
    }

    [Fact]
    public async Task UpsertManyAsync_ShouldBulkWriteEntries()
    {
        var tags = new[]
        {
            CreateDefinition("item:test-1"),
            CreateDefinition("item:test-2")
        };

        _mockCollection
            .Setup(c => c.BulkWriteAsync(
                It.IsAny<IEnumerable<WriteModel<TagDefinition>>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkWriteResult<TagDefinition>)null!);

        await _repository.UpsertManyAsync(tags, CancellationToken.None);

        _mockCollection.Verify(c => c.BulkWriteAsync(
            It.Is<IEnumerable<WriteModel<TagDefinition>>>(models => models.Count() == 2),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupFindForGetAll(IEnumerable<TagDefinition> items)
    {
        var batches = items.Any()
            ? new[] { items }
            : Array.Empty<IEnumerable<TagDefinition>>();

        var cursor = new TestAsyncCursor<TagDefinition>(batches);

        _mockCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TagDefinition>>(),
                It.IsAny<FindOptions<TagDefinition, TagDefinition>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor);
    }

    private void SetupFindForGetByCode(TagDefinition? result)
    {
        var batches = result is null
            ? Array.Empty<IEnumerable<TagDefinition>>()
            : new[] { new[] { result } as IEnumerable<TagDefinition> };

        var cursor = new TestAsyncCursor<TagDefinition>(batches);

        _mockCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TagDefinition>>(),
                It.IsAny<FindOptions<TagDefinition, TagDefinition>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor);
    }

    private sealed class TestAsyncCursor<T> : IAsyncCursor<T>
    {
        private readonly IEnumerator<IEnumerable<T>> _enumerator;
        private bool _disposed;

        public TestAsyncCursor(IEnumerable<IEnumerable<T>> batches)
        {
            _enumerator = batches?.GetEnumerator() ?? Enumerable.Empty<IEnumerable<T>>().GetEnumerator();
        }

        public IEnumerable<T> Current { get; private set; } = Enumerable.Empty<T>();

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _enumerator.Dispose();
        }

        public bool MoveNext(CancellationToken cancellationToken)
        {
            return MoveNextInternal();
        }

        public bool MoveNext()
        {
            return MoveNextInternal();
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(MoveNextInternal());
        }

        private bool MoveNextInternal()
        {
            if (!_enumerator.MoveNext())
            {
                Current = Enumerable.Empty<T>();
                return false;
            }

            Current = _enumerator.Current ?? Enumerable.Empty<T>();
            return true;
        }
    }

    private static TagDefinition CreateDefinition(string code)
    {
        return new TagDefinition
        {
            Code = code,
            Target = TagTarget.Item,
            DisplayName = code,
            Category = "Test"
        };
    }
}
