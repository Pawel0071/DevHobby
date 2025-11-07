using FluentAssertions;
using StackExchange.Redis;

namespace RPG.IntegrationTests;

public class RedisIntegrationTests : IClassFixture<TestContainersFixture>
{
    private readonly TestContainersFixture _fixture;
    private readonly IDatabase _redisDb;

    public RedisIntegrationTests(TestContainersFixture fixture)
    {
        _fixture = fixture;
        _redisDb = _fixture.RedisDatabase;
    }

    [Fact]
    public async Task ShouldConnectToRedis()
    {
        // Act
        var pong = await _redisDb.PingAsync();

        // Assert
        pong.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ShouldSetAndGetString()
    {
        // Arrange
        var key = "test:string";
        var value = "Hello Redis!";

        // Act
        await _redisDb.StringSetAsync(key, value);
        var result = await _redisDb.StringGetAsync(key);

        // Assert
        result.ToString().Should().Be(value);
    }

    [Fact]
    public async Task ShouldSetAndGetWithExpiry()
    {
        // Arrange
        var key = "test:expiry";
        var value = "expires soon";
        var ttl = TimeSpan.FromSeconds(2);

        // Act
        await _redisDb.StringSetAsync(key, value, ttl);
        var result1 = await _redisDb.StringGetAsync(key);
        
        await Task.Delay(TimeSpan.FromSeconds(3));
        
        var result2 = await _redisDb.StringGetAsync(key);

        // Assert
        result1.ToString().Should().Be(value);
        result2.IsNull.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldIncrementCounter()
    {
        // Arrange
        var key = "test:counter";

        // Act
        var value1 = await _redisDb.StringIncrementAsync(key);
        var value2 = await _redisDb.StringIncrementAsync(key);
        var value3 = await _redisDb.StringIncrementAsync(key);

        // Assert
        value1.Should().Be(1);
        value2.Should().Be(2);
        value3.Should().Be(3);
    }

    [Fact]
    public async Task ShouldStoreAndRetrieveHash()
    {
        // Arrange
        var key = "test:hash:character";
        var hashEntries = new[]
        {
            new HashEntry("name", "Hero"),
            new HashEntry("level", 10),
            new HashEntry("health", 100)
        };

        // Act
        await _redisDb.HashSetAsync(key, hashEntries);
        var name = await _redisDb.HashGetAsync(key, "name");
        var level = await _redisDb.HashGetAsync(key, "level");
        var health = await _redisDb.HashGetAsync(key, "health");

        // Assert
        name.ToString().Should().Be("Hero");
        ((long)level).Should().Be(10);
        ((long)health).Should().Be(100);
    }

    [Fact]
    public async Task ShouldWorkWithSets()
    {
        // Arrange
        var key = "test:set:players";

        // Act
        await _redisDb.SetAddAsync(key, "Player1");
        await _redisDb.SetAddAsync(key, "Player2");
        await _redisDb.SetAddAsync(key, "Player3");
        await _redisDb.SetAddAsync(key, "Player1"); // Duplicate

        var members = await _redisDb.SetMembersAsync(key);
        var isMember = await _redisDb.SetContainsAsync(key, "Player2");

        // Assert
        members.Length.Should().Be(3); // Sets don't allow duplicates
        isMember.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldWorkWithLists()
    {
        // Arrange
        var key = "test:list:events";

        // Act
        await _redisDb.ListRightPushAsync(key, "Event1");
        await _redisDb.ListRightPushAsync(key, "Event2");
        await _redisDb.ListRightPushAsync(key, "Event3");

        var length = await _redisDb.ListLengthAsync(key);
        var firstEvent = await _redisDb.ListGetByIndexAsync(key, 0);
        var allEvents = await _redisDb.ListRangeAsync(key);

        // Assert
        length.Should().Be(3);
        firstEvent.ToString().Should().Be("Event1");
        allEvents.Length.Should().Be(3);
    }

    [Fact]
    public async Task ShouldDeleteKey()
    {
        // Arrange
        var key = "test:delete";
        await _redisDb.StringSetAsync(key, "delete me");

        // Act
        var existed = await _redisDb.KeyDeleteAsync(key);
        var result = await _redisDb.StringGetAsync(key);

        // Assert
        existed.Should().BeTrue();
        result.IsNull.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldCheckKeyExists()
    {
        // Arrange
        var key = "test:exists";
        await _redisDb.StringSetAsync(key, "I exist");

        // Act
        var exists = await _redisDb.KeyExistsAsync(key);
        var notExists = await _redisDb.KeyExistsAsync("test:doesnotexist");

        // Assert
        exists.Should().BeTrue();
        notExists.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldGetTtl()
    {
        // Arrange
        var key = "test:ttl";
        var ttl = TimeSpan.FromMinutes(5);
        await _redisDb.StringSetAsync(key, "value", ttl);

        // Act
        var remainingTtl = await _redisDb.KeyTimeToLiveAsync(key);

        // Assert
        remainingTtl.Should().NotBeNull();
        remainingTtl!.Value.Should().BeLessThanOrEqualTo(ttl);
        remainingTtl!.Value.Should().BeGreaterThan(TimeSpan.FromMinutes(4));
    }
}
