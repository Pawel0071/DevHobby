using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RPG.Domain.Common;
using RPG.Domain.Common.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Common;

public class DictionaryWarmupService(IServiceProvider provider, ILogger<DictionaryWarmupService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        logger.Info("Starting dictionary warmup...");

        await Load<ItemTagDefinition>(sp, cancellationToken);
        await Load<ItemTypeDefinition>(sp, cancellationToken);
        await Load<ErrorCodeDefinition>(sp, cancellationToken);

        logger.Info("Dictionary warmup completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task Load<T>(IServiceProvider sp, CancellationToken ct)
        where T : IDictionaryEntry<T>
    {
        var repo = sp.GetRequiredService<IDictionaryRepository<T>>();
        var registry = sp.GetRequiredService<IDictionaryRegistry<T>>();
        var data = await repo.GetAllAsync(ct);
        registry.Load(data);

        logger.Debug($"Loaded dictionary: {typeof(T).Name} with {data.Count} entries");
    }
}
