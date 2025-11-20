using System.Text.Json;
using Microsoft.Extensions.Logging;
using RPG.Domain.Models.MapObjects;

namespace RPG.WorldSeeder.Seeders;

internal static class MapObjectSeedReader
{
    public static async Task<IReadOnlyList<MapObject>> LoadAsync(
        string rootPath,
        JsonSerializerOptions options,
        ILogger logger,
        CancellationToken ct)
    {
        var folder = Path.Combine(rootPath, "MapObjects");
        if (!Directory.Exists(folder))
        {
            logger.LogWarning("Seed map objects folder missing: {Folder}", folder);
            return new List<MapObject>();
        }
        var result = new List<MapObject>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await using var stream = File.OpenRead(file);
                var model = await JsonSerializer.DeserializeAsync<MapObjectSeedModel>(stream, options, ct).ConfigureAwait(false);
                if (model == null) continue;
                var location = model.Location.ToDomain();
                var mapObject = MapObject.Create(model.Name, location, model.WorldId, model.ZoneId ?? string.Empty);
                typeof(MapObject).GetProperty("Id")!.SetValue(mapObject, model.Id);
                mapObject.DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Name : model.DisplayName;
                mapObject.Description = model.Description ?? string.Empty;
                mapObject.RotationYaw = model.RotationYaw;
                mapObject.IsActive = model.IsActive;
                mapObject.Tags = model.Tags?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
                mapObject.State = model.State != null ? new Dictionary<string, string>(model.State, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                mapObject.LastUpdated = model.LastUpdated ?? DateTime.UtcNow;
                result.Add(mapObject);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to deserialize map object seed file {File}", file);
            }
        }
        logger.LogInformation("Loaded {Count} map objects", result.Count);
        return result;
    }
}
