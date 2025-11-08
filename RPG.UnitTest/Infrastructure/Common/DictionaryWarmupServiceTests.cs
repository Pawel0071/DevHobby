using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RPG.Domain.Common;
using RPG.Infrastructure.Common;
using RPG.Infrastructure.Interfaces;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Common;

public class DictionaryWarmupServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldLoadAllDictionaries()
    {
        var tagRepo = new Mock<IDictionaryRepository<ItemTagDefinition>>();
        var tagRegistry = new Mock<IDictionaryRegistry<ItemTagDefinition>>();
        var typeRepo = new Mock<IDictionaryRepository<ItemTypeDefinition>>();
        var typeRegistry = new Mock<IDictionaryRegistry<ItemTypeDefinition>>();
        var errorRepo = new Mock<IDictionaryRepository<ErrorCodeDefinition>>();
        var errorRegistry = new Mock<IDictionaryRegistry<ErrorCodeDefinition>>();
        var logger = new Mock<ILogger<DictionaryWarmupService>>();

        var tagData = new List<ItemTagDefinition> { new() { Code = "tag" } };
        var typeData = new List<ItemTypeDefinition> { new() { DisplayName = "Sword" } };
        var errorData = new List<ErrorCodeDefinition> { new() { Code = "err" } };

        tagRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tagData);
        typeRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(typeData);
        errorRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(errorData);

        var services = new ServiceCollection();
        services.AddSingleton(tagRepo.Object);
        services.AddSingleton(tagRegistry.Object);
        services.AddSingleton(typeRepo.Object);
        services.AddSingleton(typeRegistry.Object);
        services.AddSingleton(errorRepo.Object);
        services.AddSingleton(errorRegistry.Object);

    using var provider = services.BuildServiceProvider();

        var warmup = new DictionaryWarmupService(provider, logger.Object);
        await warmup.StartAsync(CancellationToken.None);

        tagRegistry.Verify(r => r.Load(tagData), Times.Once);
        typeRegistry.Verify(r => r.Load(typeData), Times.Once);
        errorRegistry.Verify(r => r.Load(errorData), Times.Once);
        logger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("Starting dictionary warmup"))), Times.Once);
        logger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("completed"))), Times.Once);
        logger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("Loaded dictionary"))), Times.Exactly(3));
    }

    [Fact]
    public Task StopAsync_ShouldCompleteImmediately()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var logger = new Mock<ILogger<DictionaryWarmupService>>();

    var warmup = new DictionaryWarmupService(services, logger.Object);
    var result = warmup.StopAsync(CancellationToken.None);

    result.Should().Be(Task.CompletedTask);
        return Task.CompletedTask;
    }
}
