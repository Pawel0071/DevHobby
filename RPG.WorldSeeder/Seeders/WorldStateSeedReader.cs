using System.Text.Json;
using Microsoft.Extensions.Logging;
using RPG.Domain.Models;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.MapObjects;

namespace RPG.WorldSeeder.Seeders;

internal static class WorldStateSeedReader
{
    public static async Task<WorldState> LoadAsync(
        string rootPath,
        JsonSerializerOptions options,
        ILogger logger,
        IReadOnlyList<RPG.Domain.Models.Npcs.Npc> npcs,
        IReadOnlyList<RPG.Domain.Models.MapObjects.MapObject> mapObjects,
        CancellationToken ct)
    {
        var folder = Path.Combine(rootPath, "WorldState");
        if (!Directory.Exists(folder))
        {
            var fallbackId = npcs.FirstOrDefault()?.WorldId ?? Guid.NewGuid();
            return WorldState.Create(fallbackId, $"seed-world-{fallbackId:N}");
        }
        var file = Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (file == null)
        {
            var fallbackId = npcs.FirstOrDefault()?.WorldId ?? Guid.NewGuid();
            return WorldState.Create(fallbackId, $"seed-world-{fallbackId:N}");
        }
        try
        {
            await using var stream = File.OpenRead(file);
            var model = await JsonSerializer.DeserializeAsync<WorldStateSeedModel>(stream, options, ct).ConfigureAwait(false);
            if (model == null)
            {
                var fallbackId = npcs.FirstOrDefault()?.WorldId ?? Guid.NewGuid();
                return WorldState.Create(fallbackId, $"seed-world-{fallbackId:N}");
            }
            var worldId = model.WorldId == Guid.Empty ? model.Id : model.WorldId;
            return WorldState.Hydrate(worldId, worldId, string.IsNullOrWhiteSpace(model.WorldName) ? $"seed-world-{worldId:N}" : model.WorldName, model.LastUpdated ?? DateTime.UtcNow, model.Characters, model.Npcs ?? npcs.Select(n => n.Id), model.MapObjects ?? mapObjects.Select(m => m.Id));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to deserialize world state seed file {File}", file);
            var fallbackId = npcs.FirstOrDefault()?.WorldId ?? Guid.NewGuid();
            return WorldState.Create(fallbackId, $"seed-world-{fallbackId:N}");
        }
    }
}
