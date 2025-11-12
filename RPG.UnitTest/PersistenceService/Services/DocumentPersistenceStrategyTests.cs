using FluentAssertions;
using Moq;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.PersistenceService.Services;

namespace RPG.UnitTest.PersistenceService.Services;

public class DocumentPersistenceStrategyTests
{
    private readonly Mock<IMongoRepository> _repositoryMock = new();

    [Fact]
    public async Task UpsertAsync_WithMatchingType_ForwardsToRepository()
    {
        var strategy = new DocumentPersistenceStrategy<PlayerDocument>(_repositoryMock.Object, PlayerDocument.CollectionName);
        var document = new PlayerDocument
        {
            Id = Guid.NewGuid(),
            Username = "tester",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsOnline = true
        };

        await strategy.UpsertAsync(document, CancellationToken.None);

        _repositoryMock.Verify(r => r.UpsertAsync(document, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_WithDifferentDocumentType_DoesNotCallRepository()
    {
        var strategy = new DocumentPersistenceStrategy<PlayerDocument>(_repositoryMock.Object, PlayerDocument.CollectionName);
        var otherDocument = new NpcDocument
        {
            Id = Guid.NewGuid(),
            Name = "npc",
            Level = 1,
            CurrentHealth = 10,
            MaxHealth = 10
        };

        await strategy.UpsertAsync(otherDocument, CancellationToken.None);

        _repositoryMock.Verify(r => r.UpsertAsync(It.IsAny<PlayerDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ForwardsToRepository()
    {
        var strategy = new DocumentPersistenceStrategy<PlayerDocument>(_repositoryMock.Object, PlayerDocument.CollectionName);
        var id = Guid.NewGuid().ToString();

        await strategy.DeleteAsync(id, CancellationToken.None);

        _repositoryMock.Verify(r => r.DeleteAsync<PlayerDocument>(id, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void CollectionName_ReturnsConfiguredValue()
    {
        const string collectionName = "Players";
        var strategy = new DocumentPersistenceStrategy<PlayerDocument>(_repositoryMock.Object, collectionName);

        strategy.CollectionName.Should().Be(collectionName);
    }
}
