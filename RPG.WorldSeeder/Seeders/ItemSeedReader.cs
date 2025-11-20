using System.Text.Json;
using Microsoft.Extensions.Logging;
using RPG.Domain.Models.Items;
using RPG.Domain.Enums;

namespace RPG.WorldSeeder.Seeders;

internal static class ItemSeedReader
{
    public static async Task<IReadOnlyDictionary<Guid, Item>> LoadAsync(
        string rootPath,
        JsonSerializerOptions options,
        ILogger logger,
        CancellationToken ct)
    {
        var folder = Path.Combine(rootPath, "Items");
        if (!Directory.Exists(folder))
        {
            logger.LogWarning("Seed items folder missing: {Folder}", folder);
            return new Dictionary<Guid, Item>();
        }

        var result = new Dictionary<Guid, Item>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await using var stream = File.OpenRead(file);
                var model = await JsonSerializer.DeserializeAsync<ItemSeedModel>(stream, options, ct).ConfigureAwait(false);
                if (model == null) continue;
                var item = new Item(model.Id, model.TypeCode)
                {
                    Name = model.Name,
                    RequiredLevel = model.RequiredLevel,
                    StackSize = model.StackSize,
                    Tags = model.Tags?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>()
                };
                if (!string.IsNullOrWhiteSpace(model.Rarity) && Enum.TryParse<ItemRarity>(model.Rarity, true, out var rarity))
                    item.Rarity = rarity;
                result[item.Id] = item;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to deserialize item seed file {File}", file);
            }
        }
        logger.LogInformation("Loaded {Count} items", result.Count);
        return result;
    }
}
