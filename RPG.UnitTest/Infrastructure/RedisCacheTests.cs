using FluentAssertions;
using Moq;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Redis;
using StackExchange.Redis;

namespace RPG.UnitTest.Infrastructure;

public class RedisCacheTests
{
    [Fact]
    public async Task GetAsync_ReturnsDeserializedValue_OnHit()
    {
        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();

        var testObj = new { Name = "abc", Value = 5 };
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(testObj);

        dbMock.Setup(d => d.StringGetAsync("key", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)json);

        multiplexerMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(dbMock.Object);

        var logger = new Mock<ILogger<RedisCache>>();
        var cache = new RedisCache(multiplexerMock.Object, logger.Object);

    var got = await cache.GetAsync<Newtonsoft.Json.Linq.JObject>("key");

    got.Should().NotBeNull();
    got!["Name"]!.ToObject<string>()!.Should().Be("abc");
    got!["Value"]!.ToObject<int>().Should().Be(5);
    }

    [Fact]
    public async Task SetAsync_CallsStringSetAsync()
    {
        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();

        multiplexerMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(dbMock.Object);

        var logger = new Mock<ILogger<RedisCache>>();
        var cache = new RedisCache(multiplexerMock.Object, logger.Object);

    await cache.SetAsync("k", new { X = 1 }, TimeSpan.FromSeconds(5));

    // Some driver versions add additional parameters; verify by checking recorded invocations for the method name and key
    var called = dbMock.Invocations.Any(i => i.Method.Name == "StringSetAsync" && i.Arguments.Count > 0 && i.Arguments[0]?.ToString() == "k");
    called.Should().BeTrue();
    }
}
