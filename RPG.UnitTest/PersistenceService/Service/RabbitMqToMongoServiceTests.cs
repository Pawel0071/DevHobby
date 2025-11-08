using FluentAssertions;
using Moq;
using RPG.Infrastructure.Interfaces;
using RPG.PersistenceService.Handlers;
using RPG.PersistenceService.Service;
using RPG.PersistenceService.Services;

namespace RPG.UnitTest.PersistenceService.Service;

public class RabbitMqToMongoServiceTests
{
    [Fact]
    public async Task StartListeningAsync_RegistersHandlerAndStartsConsuming()
    {
        var consumerMock = new Mock<IRabbitMqConsumer>();
        var loggerMock = new Mock<ILogger<RabbitMqToMongoService>>();
        var handler = CreateMessageHandler();
        Func<string, string, CancellationToken, Task>? capturedHandler = null;

        consumerMock
            .Setup(c => c.SetMessageHandler(It.IsAny<Func<string, string, CancellationToken, Task>>()))
            .Callback<Func<string, string, CancellationToken, Task>>(h => capturedHandler = h);
        consumerMock
            .Setup(c => c.StartConsumingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new RabbitMqToMongoService(consumerMock.Object, handler, loggerMock.Object);

        await service.StartListeningAsync();

        loggerMock.Verify(l => l.Info("Starting RabbitMQ to MongoDB service"), Times.Once);
        consumerMock.Verify(c => c.SetMessageHandler(It.IsAny<Func<string, string, CancellationToken, Task>>()), Times.Once);
        consumerMock.Verify(c => c.StartConsumingAsync(It.IsAny<CancellationToken>()), Times.Once);

        capturedHandler.Should().NotBeNull();
        capturedHandler!.Target.Should().BeSameAs(handler);
        capturedHandler.Method.Name.Should().Be(nameof(MessageHandler.HandleMessageAsync));
    }

    private static MessageHandler CreateMessageHandler()
    {
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<MessageHandler>>();
        var spMock = new Mock<IServiceProvider>();
        return new MessageHandler(Array.Empty<IDocumentPersistenceStrategy>(), loggerMock.Object, spMock.Object);
    }
}
