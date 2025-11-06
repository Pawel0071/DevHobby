using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MongoDB.Driver;
using RPG.Infrastructure.Repositories;

namespace RPG.UnitTest.InfrastructureTests;

public class MongoDictionaryRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsItems_FromCollection()
    {
        var dbMock = new Mock<IMongoDatabase>();
        var collectionMock = new Mock<IMongoCollection<FakeEntry>>();
        var findFluentMock = new Mock<IAsyncCursor<FakeEntry>>();

        var data = new List<FakeEntry> { new FakeEntry { Code = "a" }, new FakeEntry { Code = "b" } };

        // Setup cursor to return the data list
        findFluentMock.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        findFluentMock.Setup(c => c.Current).Returns(data);

        // For ToListAsync, we need to mock Find(...).ToCursorAsync usage; simplest is to mock collection.Find to return a IFindFluent that when ToCursorAsync returns the cursor
        var findFluentInterface = new Mock<IFindFluent<FakeEntry, FakeEntry>>();
        findFluentInterface.Setup(f => f.ToCursorAsync(It.IsAny<CancellationToken>())).ReturnsAsync(findFluentMock.Object);

        collectionMock.Setup(c => c.Find(It.IsAny<FilterDefinition<FakeEntry>>(), It.IsAny<FindOptions<FakeEntry, FakeEntry>>()))
            .Returns(findFluentInterface.Object);

        dbMock.Setup(d => d.GetCollection<FakeEntry>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(collectionMock.Object);

        var repo = new MongoDictionaryRepository<FakeEntry>(dbMock.Object);

        var result = await repo.GetAllAsync();

        result.Should().HaveCount(2);
    }

    private class FakeEntry
    {
        public string Code { get; set; } = string.Empty;
    }
}
