using System.Text.Json;
using FluentAssertions;
using Moq;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Models;
using RPG.PersistenceService.Handlers;
using RPG.PersistenceService.Services;
using MessageLogger = RPG.Infrastructure.Interfaces.ILogger<RPG.PersistenceService.Handlers.MessageHandler>;

namespace RPG.UnitTest.PersistenceService.Handlers;

public class MessageHandlerTests
{
    [Fact]
    public async Task HandleMessageAsync_WithCreateRoutingKey_InvokesUpsert()
    {
        var strategyMock = CreateStrategyMock(PlayerDocument.CollectionName);
        var handler = CreateHandler(new[] { strategyMock.Object });
        var document = CreatePlayerDocument();
        var payload = JsonSerializer.Serialize(document);

        await handler.HandleMessageAsync(payload, "player.created", CancellationToken.None);

        strategyMock.Verify(s => s.UpsertAsync(
            It.Is<PlayerDocument>(doc => doc.Id == document.Id),
            It.IsAny<CancellationToken>()),
            Times.Once);
        strategyMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleMessageAsync_WithDeletedRoutingKey_InvokesDelete()
    {
        var strategyMock = CreateStrategyMock(PlayerDocument.CollectionName);
        var handler = CreateHandler(new[] { strategyMock.Object });
        var document = CreatePlayerDocument();
        var payload = JsonSerializer.Serialize(document);

        await handler.HandleMessageAsync(payload, "player.deleted", CancellationToken.None);

        strategyMock.Verify(s => s.DeleteAsync(document.Id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        strategyMock.Verify(s => s.UpsertAsync(It.IsAny<PlayerDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleMessageAsync_WhenNoStrategyRegistered_SilentlyReturns()
    {
        var handler = CreateHandler();
        var document = CreatePlayerDocument();
        var payload = JsonSerializer.Serialize(document);

        await handler.HandleMessageAsync(payload, "player.updated", CancellationToken.None);
    }

    [Fact]
    public async Task HandleMessageAsync_WhenDeserializerReturnsNull_DoesNotInvokeStrategy()
    {
        var strategyMock = CreateStrategyMock(PlayerDocument.CollectionName);
        var handler = CreateHandler(new[] { strategyMock.Object });

        await handler.HandleMessageAsync("null", "player.created", CancellationToken.None);

        strategyMock.Verify(s => s.UpsertAsync(It.IsAny<PlayerDocument>(), It.IsAny<CancellationToken>()), Times.Never);
        strategyMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleMessageAsync_WhenRoutingKeyUnknown_ThrowsAndLogsError()
    {
        var loggerMock = new Mock<MessageLogger>();
        var handler = CreateHandler(Array.Empty<IDocumentPersistenceStrategy>(), loggerMock);

        var act = () => handler.HandleMessageAsync("{}", "unknown.created", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        loggerMock.Verify(
            l => l.Error(
                It.Is<string>(msg => msg.Contains("Error processing message")),
                It.Is<InvalidOperationException>(ex => ex.Message.Contains("unknown.created"))),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessageAsync_WhenStrategyThrows_LogsErrorAndRethrows()
    {
        var loggerMock = new Mock<MessageLogger>();
        var strategyMock = CreateStrategyMock(PlayerDocument.CollectionName);
        strategyMock
            .Setup(s => s.UpsertAsync(It.IsAny<PlayerDocument>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var handler = CreateHandler(new[] { strategyMock.Object }, loggerMock);
        var document = CreatePlayerDocument();
        var payload = JsonSerializer.Serialize(document);

        var act = () => handler.HandleMessageAsync(payload, "player.created", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        loggerMock.Verify(
            l => l.Error(
                It.Is<string>(msg => msg.Contains("Error processing message")),
                It.IsAny<InvalidOperationException>()),
            Times.Once);
    }

    private static PlayerDocument CreatePlayerDocument() => new()
    {
        Id = Guid.NewGuid(),
        Username = "tester",
        Email = "tester@example.com",
        CreatedAt = DateTime.UtcNow,
        LastLoginAt = DateTime.UtcNow,
        IsOnline = true,
        IsBanned = false
    };

    private static Mock<IDocumentPersistenceStrategy> CreateStrategyMock(string collectionName)
    {
        var strategyMock = new Mock<IDocumentPersistenceStrategy>();
        strategyMock.SetupGet(s => s.CollectionName).Returns(collectionName);
        strategyMock.Setup(s => s.UpsertAsync(It.IsAny<PlayerDocument>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        strategyMock.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return strategyMock;
    }

    private static MessageHandler CreateHandler(
        IEnumerable<IDocumentPersistenceStrategy>? strategies = null,
        Mock<MessageLogger>? loggerMock = null)
    {
        var spMock = new Mock<IServiceProvider>();
        return new MessageHandler(
            strategies ?? Array.Empty<IDocumentPersistenceStrategy>(),
            (loggerMock ?? new Mock<MessageLogger>()).Object,
            spMock.Object);
    }
}
