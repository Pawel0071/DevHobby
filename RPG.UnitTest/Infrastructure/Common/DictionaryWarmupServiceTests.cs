using System.Collections.Generic;
using System.Linq;
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
        var tagRepo = new Mock<IDictionaryRepository<TagDefinition>>();
        var tagRegistry = new Mock<IDictionaryRegistry<TagDefinition>>();
        var errorRepo = new Mock<IDictionaryRepository<ErrorCodeDefinition>>();
        var errorRegistry = new Mock<IDictionaryRegistry<ErrorCodeDefinition>>();
        var logger = new Mock<ILogger<DictionaryWarmupService>>();

        // ErrorCodeDefinition pozostaje przez repo, TagDefinition ładuje się jako static
        var errorData = new List<ErrorCodeDefinition> { new() { Code = "err" } };
        errorRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(errorData);
        errorRepo.Setup(r => r.UpsertManyAsync(It.IsAny<IEnumerable<ErrorCodeDefinition>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var services = new ServiceCollection();
        services.AddSingleton(tagRepo.Object); // nie będzie użyty w ścieżce static
        services.AddSingleton(tagRegistry.Object);
        services.AddSingleton(errorRepo.Object);
        services.AddSingleton(errorRegistry.Object);

        using var provider = services.BuildServiceProvider();

        var warmup = new DictionaryWarmupService(provider, logger.Object);
        await warmup.StartAsync(CancellationToken.None);

        tagRegistry.Verify(r => r.Load(It.Is<IEnumerable<TagDefinition>>(l => l.Any())), Times.Once);
        errorRegistry.Verify(r => r.Load(It.IsAny<IEnumerable<ErrorCodeDefinition>>()), Times.Once);
        errorRepo.Verify();
        logger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("Starting dictionary warmup"))), Times.Once);
        logger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("completed"))), Times.Once);
        logger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("Loaded"))), Times.AtLeast(2));
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
