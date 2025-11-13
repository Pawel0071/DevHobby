using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RPG.Application.Commands;
using RPG.Application.Infrastructure;
using RPG.Application.Interfaces;
using Xunit;

namespace RPG.UnitTest.Application;

public class CommandBusTests
{
    public record TestCommand(Guid Id) : ICommand;

    [Fact]
    public async Task DispatchAsync_ShouldResolveHandler_AndReturnResult_AndPropagateCancellationToken()
    {
        // Arrange
        var services = new ServiceCollection();
        var handlerMock = new Mock<ICommandHandler<TestCommand>>();
        var expected = CommandResult.Ok();
        var cts = new CancellationTokenSource();

        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()))
            .Callback((TestCommand _, CancellationToken ct) => ct.Should().Be(cts.Token))
            .ReturnsAsync(expected);

        services.AddScoped(_ => handlerMock.Object);
        var provider = services.BuildServiceProvider();
        var bus = new CommandBus(provider);

        // Act
        var result = await bus.DispatchAsync(new TestCommand(Guid.NewGuid()), cts.Token);

        // Assert
        result.Should().BeEquivalentTo(expected);
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_NoHandlerRegistered_ShouldThrow()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var bus = new CommandBus(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.DispatchAsync(new TestCommand(Guid.NewGuid())));
    }
}

