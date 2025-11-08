using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RPG.Infrastructure.Configuration;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.RabbitMQ;

namespace RPG.UnitTest.Infrastructure.Repositories.RabbitMQ;

public class RabbitMqPublisherTests
{
    private readonly Mock<IChannel> _channelMock = new();
    private readonly Mock<ILogger<RabbitMqPublisher>> _loggerMock = new();
    private readonly Mock<IActivityScope> _activityScopeMock = new();
    private readonly IDisposable _activityHandle = Mock.Of<IDisposable>();

    private readonly RabbitMqSettings _settings = new() { Host = "localhost", ExchangeName = "rpg_exchange" };

    public RabbitMqPublisherTests()
    {
        _activityScopeMock
            .Setup(scope => scope.Start(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
            .Returns(_activityHandle);
    }

    private RabbitMqPublisher CreatePublisher()
    {
        return new RabbitMqPublisher(_channelMock.Object, _loggerMock.Object, _settings, _activityScopeMock.Object);
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishMessageAndLogInfo()
    {
        // Arrange
        var publisher = CreatePublisher();
        var topic = "item.created";
        var message = new { Id = Guid.NewGuid(), Name = "Sword" };
        var expectedJson = JsonConvert.SerializeObject(message);
        var expectedBody = Encoding.UTF8.GetBytes(expectedJson);

        // Act
        await publisher.PublishAsync(topic, message);

        // Assert — ręczna weryfikacja wywołania
        var invocation = _channelMock.Invocations
            .Single(i => i.Method.Name == nameof(IChannel.BasicPublishAsync));

        invocation.Arguments[0].Should().Be("rpg_exchange"); // exchange
        invocation.Arguments[1].Should().Be(topic); // routingKey
        invocation.Arguments[2].Should().Be(false); // mandatory
        invocation.Arguments[3].Should().NotBeNull(); // zamiast BeAssignableTo<IBasicProperties>
        ((ReadOnlyMemory<byte>)invocation.Arguments[4]).Span.SequenceEqual(expectedBody).Should().BeTrue(); // body
        invocation.Arguments[5].Should().Be(CancellationToken.None); // cancellationToken

        _loggerMock.Verify(l => l.Info(It.Is<string>(s => s.Contains("RabbitMqPublisher initialized"))), Times.Once);
        _loggerMock.Verify(l => l.Debug(It.Is<string>(s => s.Contains(topic))), Times.Once);
        _loggerMock.Verify(l => l.Info(It.Is<string>(s => s.Contains("published"))), Times.Once);
        _loggerMock.VerifyNoOtherCalls();
    }
}
