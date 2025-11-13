using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RPG.Domain.Enums;
using RPG.Infrastructure.Helpers;
using RPG.Infrastructure.Interfaces;
using RPG.PersistenceService.Services;
using RedisWarmUp.Services;
using RPG.Domain.Models.Items;
using RPG.Infrastructure.Models;
using WarmUpStrategy = RedisWarmUp.Services.IDocumentWarmUpStrategy;

namespace RPG.CLI.FunctionalTests;

/// <summary>
///     Drives the functional verification pipeline that spans Infrastructure, PersistenceService and RedisWarmUp.
/// </summary>
internal sealed class FunctionalTestRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FunctionalTestRunner> _logger;

    public FunctionalTestRunner(IServiceProvider serviceProvider, ILogger<FunctionalTestRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<int> RunAsync(string samplePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(samplePath))
        {
            _logger.Error($"Sample file '{samplePath}' not found.");
            return 1;
        }

        var sampleJson = await File.ReadAllTextAsync(samplePath, cancellationToken);
        var sample = JsonSerializer.Deserialize<ItemSample>(sampleJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (sample is null)
        {
            _logger.Error("Failed to deserialize sample JSON.");
            return 1;
        }

        var item = BuildDomainItem(sample);

        using var scope = _serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var documentRepository = services.GetRequiredService<IModelRepository>();
        var mongoRepository = services.GetRequiredService<IMongoRepository>();
        var redisRepository = services.GetRequiredService<IRedisRepository>();
    var warmUpStrategies = services.GetServices<WarmUpStrategy>();
        var warmUpLogger = services.GetRequiredService<ILogger<RedisWarmUpService>>();

        await mongoRepository.DeleteAsync<ItemDocument>(item.Id, cancellationToken);
        await redisRepository.DeleteAsync<ItemDocument>(item.Id, cancellationToken);

        _logger.Info($"Running infrastructure pipeline for Item '{item.Name}' ({item.Id}).");
        await documentRepository.UpsertAsync(item, cancellationToken);

        var persistedDocument = await mongoRepository.GetByIdAsync<ItemDocument>(item.Id, cancellationToken);
        if (persistedDocument is null)
        {
            _logger.Error("Persistence verification failed: Mongo repository returned null document.");
            return 1;
        }

    _logger.Info("Item successfully persisted via Mongo repository.");

        // Ensure Redis warm-up repopulates cache by clearing any existing entry
        await redisRepository.DeleteAsync<ItemDocument>(item.Id, cancellationToken);

        var itemWarmUpStrategy = warmUpStrategies.FirstOrDefault(strategy =>
            string.Equals(strategy.CollectionName, ItemDocument.CollectionName, StringComparison.OrdinalIgnoreCase));

        if (itemWarmUpStrategy is null)
        {
            _logger.Error("Warm-up verification failed: strategy for ItemDocument not registered.");
            return 1;
        }

        _logger.Info("Executing Redis warm-up strategy for Item document.");
        await itemWarmUpStrategy.WarmUpAsync(redisRepository, warmUpLogger, cancellationToken);

        var cachedDocument = await redisRepository.GetByIdAsync<ItemDocument>(item.Id, cancellationToken);
        if (cachedDocument is null)
        {
            _logger.Error("Warm-up verification failed: Redis repository returned null document.");
            return 1;
        }

        _logger.Info("Redis warm-up successfully restored the item document to cache.");

        Console.WriteLine("\n🚀 Functional test summary");
        Console.WriteLine(" - Infrastructure ModelRepository upsert ✔");
        Console.WriteLine(" - Persistence message handling to Mongo ✔");
        Console.WriteLine(" - Redis warm-up pipeline ✔\n");

        return 0;
    }

    private static Item BuildDomainItem(ItemSample sample)
    {
        var itemId = sample.Id ?? Guid.NewGuid();
        var item = new Item(itemId, sample.TypeCode ?? "functional.test")
        {
            Name = sample.Name ?? "CLI Sample Item",
            Rarity = ParseEnum(sample.Rarity, ItemRarity.Common),
            RequiredLevel = sample.RequiredLevel,
            StackSize = sample.StackSize,
            Tags = sample.Tags is null ? new HashSet<string>() : new HashSet<string>(sample.Tags, StringComparer.OrdinalIgnoreCase)
        };

        return item;
    }

    private static ItemRarity ParseEnum(string? value, ItemRarity fallback)
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<ItemRarity>(value, true, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private sealed record ItemSample
    {
        public string? Entity { get; init; }
        public Guid? Id { get; init; }
        public string? Name { get; init; }
        public string? Rarity { get; init; }
        public string? TypeCode { get; init; }
        public int RequiredLevel { get; init; } = 1;
        public int StackSize { get; init; } = 1;
        public IReadOnlyCollection<string>? Tags { get; init; }
    }
}
