using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RPG.Domain.Models; // WorldState
using RPG.Domain.Models.Items;
using RPG.Domain.Models.MapObjects;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Skills;

namespace RPG.WorldSeeder.Seeders;

/// <summary>
/// Orchestrates loading of seed data via specialized reader classes.
/// </summary>
internal sealed class SeedDataLoader
{
    private readonly ILogger<SeedDataLoader> _logger;
    private readonly string _rootPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public SeedDataLoader(IHostEnvironment environment, ILogger<SeedDataLoader> logger)
    {
        _logger = logger;
        var preferredRoot = Path.Combine(environment.ContentRootPath, "SeedData");
        _rootPath = Directory.Exists(preferredRoot) ? preferredRoot : Path.Combine(AppContext.BaseDirectory, "SeedData");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<SeedDataSet> LoadAsync(CancellationToken cancellationToken)
    {
        var items = await ItemSeedReader.LoadAsync(_rootPath, _jsonOptions, _logger, cancellationToken).ConfigureAwait(false);
        var skills = await SkillSeedReader.LoadAsync(_rootPath, _jsonOptions, _logger, cancellationToken).ConfigureAwait(false);
        var mapObjects = await MapObjectSeedReader.LoadAsync(_rootPath, _jsonOptions, _logger, cancellationToken).ConfigureAwait(false);
        var npcs = await NpcSeedReader.LoadAsync(_rootPath, _jsonOptions, _logger, skills, items, cancellationToken).ConfigureAwait(false);
        var worldState = await WorldStateSeedReader.LoadAsync(_rootPath, _jsonOptions, _logger, npcs, mapObjects, cancellationToken).ConfigureAwait(false);
        return new SeedDataSet(items, skills, npcs, mapObjects, worldState);
    }
}
