using System.Text;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Rabbit;

namespace RPG.UnitTest.Infrastructure;

public class RabbitPublisherTests
{
    private readonly Mock<IChannel> _channelMock = new();
    private readonly Mock<ILogger<RabbitPublisher>> _loggerMock = new();
    private readonly RabbitMqSettings _settings = new()
    {
        Host = "localhost",
        ExchangeName = "rpg_exchange"
    };

    private RabbitPublisher CreatePublisher() =>
        new RabbitPublisher(_channelMock.Object, _loggerMock.Object, _settings);

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

        _loggerMock.Verify(l => l.Debug(It.Is<string>(s => s.Contains(topic))), Times.Once);
        _loggerMock.Verify(l => l.Info(It.Is<string>(s => s.Contains("published"))), Times.Once);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishAsync_ShouldLogErrorAndRethrow_WhenPublishFails()
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

        Console.WriteLine(invocation.Arguments[3].GetType().FullName);

        invocation.Arguments[0].Should().Be("rpg_exchange"); // exchange
        invocation.Arguments[1].Should().Be(topic); // routingKey
        invocation.Arguments[2].Should().Be(false); // mandatory
        invocation.Arguments[3].Should().NotBeNull(); // properties
        ((ReadOnlyMemory<byte>)invocation.Arguments[4]).Span.SequenceEqual(expectedBody).Should().BeTrue(); // body
        invocation.Arguments[5].Should().Be(CancellationToken.None); // cancellationToken

        _loggerMock.Verify(l => l.Debug(It.Is<string>(s => s.Contains(topic))), Times.Once);
        _loggerMock.Verify(l => l.Info(It.Is<string>(s => s.Contains("published"))), Times.Once);
        _loggerMock.VerifyNoOtherCalls();
    }
}