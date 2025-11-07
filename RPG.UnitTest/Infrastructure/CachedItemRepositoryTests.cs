using FluentAssertions;
using MongoDB.Driver;
using Moq;
using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.Redis;

namespace RPG.UnitTest.Infrastructure;

public class CachedItemRepositoryTests
{
    private readonly Mock<IRedisCache> _mockRedis;
    private readonly Mock<IMongoCollection<ItemDocument>> _mockMongo;
    private readonly Mock<IRabbitPublisher> _mockRabbit;
    private readonly Mock<ILogger<CachedItemRepository>> _mockLogger;
    private readonly CachedItemRepository _repository;

    public CachedItemRepositoryTests()
    {
        _mockRedis = new Mock<IRedisCache>();
        _mockMongo = new Mock<IMongoCollection<ItemDocument>>();
        _mockRabbit = new Mock<IRabbitPublisher>();
        _mockLogger = new Mock<ILogger<CachedItemRepository>>();
        
        _repository = new CachedItemRepository(
            _mockRedis.Object,
            _mockMongo.Object,
            _mockRabbit.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFromCache_WhenItemExistsInCache()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var cachedItem = new Item(itemId, "weapon") { Name = "Cached Sword" };
        
        _mockRedis.Setup(x => x.GetAsync<Item>(It.IsAny<string>()))
            .ReturnsAsync(cachedItem);

        // Act
        var result = await _repository.GetByIdAsync(itemId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(itemId);
        result.Name.Should().Be("Cached Sword");
        
        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("found in cache"))), Times.Once);
        _mockMongo.Verify(x => x.FindAsync(It.IsAny<FilterDefinition<ItemDocument>>(), It.IsAny<FindOptions<ItemDocument, ItemDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_ShouldCacheAndPublish()
    {
        // Arrange
        var item = new Item(Guid.NewGuid(), "weapon") { Name = "Test Sword" };

        _mockRedis.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Item>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _mockRabbit.Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<Item>()))
            .Returns(Task.CompletedTask);

        // Act
        await _repository.SaveAsync(item);

        // Assert
        _mockRedis.Verify(x => x.SetAsync(
            It.Is<string>(s => s.Contains(item.Id.ToString())), 
            item, 
            It.IsAny<TimeSpan>()), 
            Times.Once);
        
        _mockRabbit.Verify(x => x.PublishAsync("item.save", item), Times.Once);
        
        _mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("Saving item"))), Times.Once);
        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Caching item"))), Times.Once);
        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Publishing item save event"))), Times.Once);
    }

    // Note: GetByIdAsync with null result and GetByNameAsync tests are skipped
    // because they require mocking IFindFluent.FirstOrDefaultAsync which is an extension method
    // These scenarios are better tested with integration tests using real MongoDB instance
}
