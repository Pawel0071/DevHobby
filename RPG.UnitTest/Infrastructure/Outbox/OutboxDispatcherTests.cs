using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MongoDB.Bson;
using MongoDB.Driver;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Outbox;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Outbox;

public class OutboxDispatcherTests
{
    private readonly Mock<IMongoCollection<OutboxMessage>> _collection = new();
    private readonly Mock<IRabbitMqPublisher> _publisher = new();
    private readonly Mock<ILogger<OutboxDispatcher>> _logger = new();

    [Fact]
    public async Task ExecuteAsync_WhenCancelledInitially_ShouldLogStop()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var dispatcher = new OutboxDispatcher(_collection.Object, _publisher.Object, _logger.Object);
        var execute = typeof(OutboxDispatcher).GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        execute.Should().NotBeNull();

        var task = (Task)execute!.Invoke(dispatcher, new object[] { cts.Token })!;
        await task;

        _logger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("OutboxDispatcher started"))), Times.Once);
        _logger.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("OutboxDispatcher stopped"))), Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_WhenPublishSucceeds_ShouldMarkAsSent()
    {
        var message = new OutboxMessage { Topic = "quests.updated", Payload = "{}" };

        _publisher.Setup(p => p.PublishAsync(message.Topic, message.Payload))
            .Returns(Task.CompletedTask);

        _collection.Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<OutboxMessage>>(),
                It.IsAny<UpdateDefinition<OutboxMessage>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<UpdateResult>());

        var dispatcher = new OutboxDispatcher(_collection.Object, _publisher.Object, _logger.Object);
        var process = typeof(OutboxDispatcher).GetMethod("ProcessMessage", BindingFlags.Instance | BindingFlags.NonPublic);
        process.Should().NotBeNull();

        var task = (Task)process!.Invoke(dispatcher, new object[] { message, CancellationToken.None })!;
        await task;

        _publisher.Verify(p => p.PublishAsync(message.Topic, message.Payload), Times.Once);
        _collection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<OutboxMessage>>(),
            It.IsAny<UpdateDefinition<OutboxMessage>>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        _logger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("dispatched successfully"))), Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_WhenPublishFails_ShouldIncrementRetryCount()
    {
        var message = new OutboxMessage { Topic = "quests.failed", Payload = "{}" };
        var exception = new InvalidOperationException("broken channel");

        _publisher.Setup(p => p.PublishAsync(message.Topic, message.Payload))
            .ThrowsAsync(exception);

        _collection.Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<OutboxMessage>>(),
                It.IsAny<UpdateDefinition<OutboxMessage>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<UpdateResult>());

        var dispatcher = new OutboxDispatcher(_collection.Object, _publisher.Object, _logger.Object);
        var process = typeof(OutboxDispatcher).GetMethod("ProcessMessage", BindingFlags.Instance | BindingFlags.NonPublic);
        process.Should().NotBeNull();

        var task = (Task)process!.Invoke(dispatcher, new object[] { message, CancellationToken.None })!;
        await task;

        _collection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<OutboxMessage>>(),
            It.IsAny<UpdateDefinition<OutboxMessage>>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        _logger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Failed to dispatch")), exception), Times.Once);
    }
}
