using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RPG.Application.Commands;
using RPG.Application.Interfaces;
using RPG.Application.Infrastructure;
using RPG.Abstractions.Interfaces;
using Xunit;

namespace RPG.UnitTest.Application;

public class CommandBusTests
{
    [Fact]
    public async Task Publish_Should_Invoke_Handler_And_Return_Result()
    {
        var handlerMock = new Mock<ICommandHandler<GainExperienceCommand>>();
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<GainExperienceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());

        // Mock wewnętrznego ServiceProvider dla scope
        var scopedSpMock = new Mock<IServiceProvider>();
        scopedSpMock
            .Setup(s => s.GetService(typeof(ICommandHandler<GainExperienceCommand>)))
            .Returns(handlerMock.Object);

        // Mock scope i fabryki scope
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.SetupGet(s => s.ServiceProvider).Returns(scopedSpMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        // Główny SP zwraca fabrykę scope
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);

        var bus = new CommandBus(spMock.Object);
        var cmd = new GainExperienceCommand(Guid.NewGuid(), 50) { Metadata = new CommandMetadata(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow) };
        var result = await bus.DispatchAsync(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<GainExperienceCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
