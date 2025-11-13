using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Outbox;
using StackExchange.Redis;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Outbox;

public class OutboxDispatcherTests
{
    private readonly Mock<IConnectionMultiplexer> _multiplexer = new();
    private readonly Mock<IDatabase> _database = new();
    private readonly Mock<IRabbitMqPublisher> _publisher = new();
    private readonly Mock<ILogger<OutboxDispatcher>> _logger = new();

    public OutboxDispatcherTests()
    {
        _multiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_database.Object);
    }

    private sealed class TestBreakerState : IOutboxCircuitBreakerState
    {
        public string State { get; private set; } = "Closed";
        public DateTime ChangedAtUtc { get; private set; } = DateTime.UtcNow;
        public int RecentErrorCount { get; private set; }
        public void SetState(string state, DateTime changedAtUtc)
        {
            State = state;
            ChangedAtUtc = changedAtUtc;
        }
        public void SetRecentErrorCount(int count) => RecentErrorCount = count;
    }

    private OutboxDispatcher CreateDispatcher() => new(_multiplexer.Object, _publisher.Object, _logger.Object, new TestBreakerState());

    [Fact]
    public async Task ExecuteAsync_WhenCancelledInitially_ShouldLogStop()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var dispatcher = CreateDispatcher();
        var execute = typeof(OutboxDispatcher).GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        execute.Should().NotBeNull();

        var task = (Task)execute!.Invoke(dispatcher, new object[] { cts.Token })!;
        await task;

        _logger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("OutboxDispatcher started"))), Times.Once);
        _logger.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("OutboxDispatcher stopped"))), Times.Once);
    }

    [Fact]
    public async Task Publish_SingleMessage_Success_ShouldNotRetry()
    {
        var dispatcher = CreateDispatcher();
        var msg = new OutboxMessage { Topic = "quests.updated", Payload = "{}" };
        _publisher.Setup(p => p.PublishAsync(msg.Topic, msg.Payload)).Returns(Task.CompletedTask);

        // invoke private TryPublishAsync
        var method = typeof(OutboxDispatcher).GetMethod("TryPublishAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        var resultTask = (Task<bool>)method!.Invoke(dispatcher, new object[] { msg, CancellationToken.None })!;
        var success = await resultTask;
        success.Should().BeTrue();
        _publisher.Verify(p => p.PublishAsync(msg.Topic, msg.Payload), Times.Once);
        _database.Verify(db => db.ListLeftPushAsync("outbox:retry", It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task Publish_Failure_ShouldEnqueueRetry()
    {
        var dispatcher = CreateDispatcher();
        var msg = new OutboxMessage { Topic = "quests.failed", Payload = "{}" };
        _publisher.Setup(p => p.PublishAsync(msg.Topic, msg.Payload)).ThrowsAsync(new InvalidOperationException("broker down"));

        var tryPublish = typeof(OutboxDispatcher).GetMethod("TryPublishAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        var handleFailure = typeof(OutboxDispatcher).GetMethod("HandlePublishFailureAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        tryPublish.Should().NotBeNull();
        handleFailure.Should().NotBeNull();

        var success = await (Task<bool>)tryPublish!.Invoke(dispatcher, new object[] { msg, CancellationToken.None })!;
        success.Should().BeFalse();

        _database.Setup(db => db.ListLeftPushAsync("outbox:retry", It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        await (Task)handleFailure!.Invoke(dispatcher, new object[] { msg, CancellationToken.None })!;

        _database.Verify(db => db.ListLeftPushAsync("outbox:retry", It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }
}
