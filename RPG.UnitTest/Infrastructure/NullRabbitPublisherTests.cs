using FluentAssertions;
using Moq;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.RabbitMQ;

namespace RPG.UnitTest.Infrastructure;

public class NullRabbitPublisherTests
{
    [Fact]
    public void Constructor_ShouldLogInitialization_WhenLoggerProvided()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<NullRabbitPublisher>>();

        // Act
        var publisher = new NullRabbitPublisher(mockLogger.Object);

        // Assert
        mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("NullRabbitPublisher initialized"))), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenLoggerIsNull()
    {
        // Act
        var act = () => new NullRabbitPublisher(null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task PublishAsync_ShouldNotThrow()
    {
        // Arrange
        var publisher = new NullRabbitPublisher();
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
        var mockLogger = new Mock<ILogger<NullRabbitPublisher>>();
        var publisher = new NullRabbitPublisher(mockLogger.Object);
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
        var publisher = new NullRabbitPublisher();

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
        var publisher = new NullRabbitPublisher();

        // Act
        var act = async () => await publisher.PublishAsync<object?>("topic", null);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
