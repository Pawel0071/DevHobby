using FluentAssertions;
using MongoDB.Driver;
using Moq;
using RPG.Domain.Common;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.MongoDB;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPG.Infrastructure.Repositories.Orchestrators;
using Xunit;

namespace RPG.UnitTest.Infrastructure;

public class DictionaryRepositoryTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<ItemTagDefinition>> _mockCollection;
    private readonly Mock<ILogger<DictionaryRepository<ItemTagDefinition>>> _mockLogger;
    private readonly DictionaryRepository<ItemTagDefinition> _repository;
    private readonly Mock<IAsyncCursor<ItemTagDefinition>> _mockCursor;

    public DictionaryRepositoryTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<ItemTagDefinition>>();
        _mockLogger = new Mock<ILogger<DictionaryRepository<ItemTagDefinition>>>();
        _mockCursor = new Mock<IAsyncCursor<ItemTagDefinition>>();

        _mockDatabase
            .Setup(db => db.GetCollection<ItemTagDefinition>(It.IsAny<string>(), null))
            .Returns(_mockCollection.Object);

        _repository = new DictionaryRepository<ItemTagDefinition>(_mockDatabase.Object, _mockLogger.Object);
    }

    private void SetupMockCursor(IReadOnlyList<ItemTagDefinition> items)
    {
        _mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        _mockCursor.Setup(c => c.Current).Returns(items);

        _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<ItemTagDefinition>>(),
                It.IsAny<FindOptions<ItemTagDefinition, ItemTagDefinition>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockCursor.Object);
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyCollection_ShouldReturnEmptyList()
    {
        // Arrange
        SetupMockCursor(new List<ItemTagDefinition>());

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Loaded 0"))), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleItems_ShouldReturnAllItems()
    {
        // Arrange
        var tags = new List<ItemTagDefinition>
        {
            new() { Code = "weapon", DisplayName = "Weapon", Category = "Equipment" },
            new() { Code = "consumable", DisplayName = "Consumable", Category = "Usable" },
            new() { Code = "quest", DisplayName = "Quest Item", Category = "Special" }
        };
        SetupMockCursor(tags);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.Code == "weapon");
        result.Should().Contain(t => t.Code == "consumable");
        result.Should().Contain(t => t.Code == "quest");
        _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Loaded 3"))), Times.Once);
    }

    [Fact]
    public async Task GetByCodeAsync_WithExistingCode_ShouldReturnItem()
    {
        // Arrange
        var expectedTag = new ItemTagDefinition
        {
            Code = "armor",
            DisplayName = "Armor",
            Category = "Equipment",
            Description = "Protective gear"
        };
        SetupMockCursor(new List<ItemTagDefinition> { expectedTag });

        // Act
        var result = await _repository.GetByCodeAsync("armor");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("armor");
        result.DisplayName.Should().Be("Armor");
    }

    [Fact]
    public async Task GetByCodeAsync_WithNonExistingCode_ShouldReturnNull()
    {
        // Arrange
        SetupMockCursor(new List<ItemTagDefinition>());

        // Act
        var result = await _repository.GetByCodeAsync("nonexistent");

        // Assert
        result.Should().BeNull();
        _mockLogger.Verify(l => l.Warn(It.Is<string>(s => s.Contains("not found"))), Times.Once);
    }
}
