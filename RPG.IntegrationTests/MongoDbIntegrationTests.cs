using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace RPG.IntegrationTests;

public class MongoDbIntegrationTests : IClassFixture<TestContainersFixture>
{
    private readonly TestContainersFixture _fixture;
    private readonly IMongoCollection<BsonDocument> _testCollection;

    public MongoDbIntegrationTests(TestContainersFixture fixture)
    {
        _fixture = fixture;
        _testCollection = _fixture.MongoDatabase.GetCollection<BsonDocument>("test_collection");
    }

    [Fact]
    public async Task ShouldConnectToMongoDb()
    {
        // Act
        var result = await _fixture.MongoDatabase.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

        // Assert
        result.Should().NotBeNull();
        result["ok"].AsDouble.Should().Be(1.0);
    }

    [Fact]
    public async Task ShouldInsertDocument()
    {
        // Arrange
        var document = new BsonDocument
        {
            { "name", "TestCharacter" },
            { "level", 10 },
            { "health", 100 }
        };

        // Act
        await _testCollection.InsertOneAsync(document);

        // Assert
        var result = await _testCollection.Find(d => d["name"] == "TestCharacter").FirstOrDefaultAsync();
        result.Should().NotBeNull();
        result["name"].AsString.Should().Be("TestCharacter");
        result["level"].AsInt32.Should().Be(10);
        result["health"].AsInt32.Should().Be(100);
    }

    [Fact]
    public async Task ShouldReadDocument()
    {
        // Arrange
        var document = new BsonDocument
        {
            { "name", "ReadTest" },
            { "value", 42 }
        };
        await _testCollection.InsertOneAsync(document);

        // Act
        var result = await _testCollection.Find(d => d["name"] == "ReadTest").FirstOrDefaultAsync();

        // Assert
        result.Should().NotBeNull();
        result["value"].AsInt32.Should().Be(42);
    }

    [Fact]
    public async Task ShouldUpdateDocument()
    {
        // Arrange
        var document = new BsonDocument
        {
            { "name", "UpdateTest" },
            { "value", 10 }
        };
        await _testCollection.InsertOneAsync(document);

        // Act
        var filter = Builders<BsonDocument>.Filter.Eq("name", "UpdateTest");
        var update = Builders<BsonDocument>.Update.Set("value", 20);
        await _testCollection.UpdateOneAsync(filter, update);

        // Assert
        var result = await _testCollection.Find(d => d["name"] == "UpdateTest").FirstOrDefaultAsync();
        result.Should().NotBeNull();
        result["value"].AsInt32.Should().Be(20);
    }

    [Fact]
    public async Task ShouldDeleteDocument()
    {
        // Arrange
        var document = new BsonDocument
        {
            { "name", "DeleteTest" },
            { "value", 99 }
        };
        await _testCollection.InsertOneAsync(document);

        // Act
        var filter = Builders<BsonDocument>.Filter.Eq("name", "DeleteTest");
        await _testCollection.DeleteOneAsync(filter);

        // Assert
        var result = await _testCollection.Find(d => d["name"] == "DeleteTest").FirstOrDefaultAsync();
        result.Should().BeNull();
    }

    [Fact]
    public async Task ShouldQueryWithFilter()
    {
        // Arrange
        await _testCollection.InsertManyAsync(new[]
        {
            new BsonDocument { { "type", "warrior" }, { "level", 10 } },
            new BsonDocument { { "type", "warrior" }, { "level", 15 } },
            new BsonDocument { { "type", "mage" }, { "level", 12 } }
        });

        // Act
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("type", "warrior"),
            Builders<BsonDocument>.Filter.Gt("level", 10)
        );
        var results = await _testCollection.Find(filter).ToListAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0]["level"].AsInt32.Should().Be(15);
    }
}
