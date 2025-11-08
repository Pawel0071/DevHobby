using FluentAssertions;
using Moq;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.RabbitMQ;
using System;
using System.Threading.Tasks;

namespace RPG.UnitTest.Infrastructure.Repositories.RabbitMQ;

public class NullRabbitMqPublisherTests
{
    [Fact]
    public void Constructor_ShouldLogInitialization_WhenLoggerProvided()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<NullRabbitMqPublisher>>();

        // Act
        var publisher = new NullRabbitMqPublisher(mockLogger.Object);

        // Assert
        mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("NullRabbitMqPublisher initialized"))), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenLoggerIsNull()
    {
        // Act
        var act = () => new NullRabbitMqPublisher(null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task PublishAsync_ShouldNotThrow()
    {
        // Arrange
        var publisher = new NullRabbitMqPublisher(null);
        var message = new { Id = 1, Name = "Test" };

        // Act
        var act = async () => await publisher.PublishAsync("test.topic", message);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_ShouldLogDebugMessage_WhenLoggerProvided()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<NullRabbitMqPublisher>>();
        var publisher = new NullRabbitMqPublisher(mockLogger.Object);
        var message = new { Id = 1, Name = "Test" };

        // Act
        await publisher.PublishAsync("test.topic", message);

        // Assert
        mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Skipping message publish"))), Times.Once);
        mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("test.topic"))), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldCompleteImmediately()
    {
        // Arrange
        var publisher = new NullRabbitMqPublisher(null);

        // Act
        var startTime = DateTime.UtcNow;
        await publisher.PublishAsync("topic", "message");
        var endTime = DateTime.UtcNow;

        // Assert
        (endTime - startTime).Should().BeLessThan(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task PublishAsync_ShouldHandleNullMessage()
    {
        // Arrange
        var publisher = new NullRabbitMqPublisher(null);

        // Act
        var act = async () => await publisher.PublishAsync<object?>("topic", null);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
