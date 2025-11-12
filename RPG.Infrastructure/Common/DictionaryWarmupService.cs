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

        await Load<TagDefinition>(sp, cancellationToken);
        await Load<ErrorCodeDefinition>(sp, cancellationToken);

        logger.Info("Dictionary warmup completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task Load<T>(IServiceProvider sp, CancellationToken ct)
        where T : IDictionaryEntry<T>
    {
        var registry = sp.GetRequiredService<IDictionaryRegistry<T>>();

        var isStatic = typeof(IStaticDictionaryDefinition).IsAssignableFrom(typeof(T)) && typeof(T) != typeof(ErrorCodeDefinition);
        if (isStatic)
        {
            registry.Load(T.Predefined.ToList());
            logger.Debug($"Loaded static dictionary: {typeof(T).Name} with {T.Predefined.Count()} entries (in-memory)");
            return;
        }

        var repo = sp.GetRequiredService<IDictionaryRepository<T>>();
        try
        {
            await repo.UpsertManyAsync(T.Predefined, ct);
            var data = await repo.GetAllAsync(ct);
            registry.Load(data);
            logger.Debug($"Loaded dictionary: {typeof(T).Name} with {data.Count} entries");
        }
        catch (FormatException ex)
        {
            logger.Error($"Failed format loading {typeof(T).Name}, falling back to predefined", ex);
            registry.Load(T.Predefined.ToList());
        }
    }
}
