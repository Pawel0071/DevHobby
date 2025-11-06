using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using RPG.Infrastructure.Redis;

namespace RPG.UnitTest.InfrastructureTests;

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

        var got = await cache.GetAsync<dynamic>("key");

        ((string)got.Name).Should().Be("abc");
        ((long)got.Value).Should().Be(5);
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

        dbMock.Verify(d => d.StringSetAsync("k", It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }
}
