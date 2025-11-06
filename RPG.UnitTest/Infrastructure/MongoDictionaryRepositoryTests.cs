using FluentAssertions;
using MongoDB.Driver;
using Moq;
using RPG.Infrastructure.Repositories;

namespace RPG.UnitTest.Infrastructure;

public class MongoDictionaryRepositoryTests
{
    [Fact]
    public void Ctor_ShouldCreateRepository_WhenDatabaseProvidesCollection()
    {
        var dbMock = new Mock<IMongoDatabase>();
        var collectionMock = new Mock<IMongoCollection<FakeEntry>>();

        dbMock.Setup(d => d.GetCollection<FakeEntry>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(collectionMock.Object);

        var repo = new MongoDictionaryRepository<FakeEntry>(dbMock.Object);

        repo.Should().NotBeNull();
    }
}

public class FakeEntry
{
    public string Code { get; set; } = string.Empty;
}
