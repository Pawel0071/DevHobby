using MongoDB.Driver;
using RPG.Core.Interfaces;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Entities.Skills;
using RPG.Infrastructure.Helpers;
using RPG.Infrastructure.Interfaces;
using RPG.WorldSeeder.Seeders;

namespace RPG.WorldSeeder.Services;

internal sealed class WorldSeederService
{
    private static readonly HashSet<string> SeededEntityKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "item",
        "skill",
        "npc",
        "mapobject",
        "worldstate"
    };

    private readonly SeedDataLoader _seedDataLoader;
    private readonly IModelRepository _modelRepository;
    private readonly IWorldStateService _worldStateService;
    private readonly IMongoDatabase _mongoDatabase;
    private readonly ILogger<WorldSeederService> _logger;

    public WorldSeederService(
        SeedDataLoader seedDataLoader,
        IModelRepository modelRepository,
        IWorldStateService worldStateService,
        IMongoDatabase mongoDatabase,
        ILogger<WorldSeederService> logger)
    {
        _seedDataLoader = seedDataLoader;
        _modelRepository = modelRepository;
        _worldStateService = worldStateService;
        _mongoDatabase = mongoDatabase;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
    _logger.Info("Preparing world seed data from JSON definitions...");

        await ClearMongoCollectionsAsync(cancellationToken).ConfigureAwait(false);

        var data = await _seedDataLoader.LoadAsync(cancellationToken).ConfigureAwait(false);

        await SeedItemsAsync(data.Items.Values, cancellationToken).ConfigureAwait(false);
        await SeedSkillsAsync(data.Skills.Values, cancellationToken).ConfigureAwait(false);
        await SeedMapObjectsAsync(data.MapObjects, cancellationToken).ConfigureAwait(false);
        await SeedNpcsAsync(data.Npcs, cancellationToken).ConfigureAwait(false);

    var worldState = PrepareWorldState(data.WorldState, data.Npcs, data.MapObjects);
        await _modelRepository.UpsertAsync(worldState, cancellationToken).ConfigureAwait(false);

        _logger.Info(
            $"World seeding completed. Items: {data.Items.Count}, Skills: {data.Skills.Count}, NPCs: {data.Npcs.Count}, MapObjects: {data.MapObjects.Count}.");
    }

    private async Task SeedItemsAsync(IEnumerable<Item> items, CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _modelRepository.UpsertAsync(item, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SeedSkillsAsync(IEnumerable<Skill> skills, CancellationToken cancellationToken)
    {
        foreach (var skill in skills)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _modelRepository.UpsertAsync(skill, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SeedMapObjectsAsync(IEnumerable<MapObject> mapObjects, CancellationToken cancellationToken)
    {
        foreach (var mapObject in mapObjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _modelRepository.UpsertAsync(mapObject, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SeedNpcsAsync(IEnumerable<Npc> npcs, CancellationToken cancellationToken)
    {
        foreach (var npc in npcs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _modelRepository.UpsertAsync(npc, cancellationToken).ConfigureAwait(false);
        }
    }

    private WorldState PrepareWorldState(WorldState worldState, IReadOnlyList<Npc> npcs, IReadOnlyList<MapObject> mapObjects)
    {
        typeof(WorldState).GetProperty("Id")?.SetValue(worldState, worldState.WorldId);

        worldState.Characters.Clear();
        worldState.Npcs.Clear();
        worldState.MapObjects.Clear();

        foreach (var npc in npcs)
        {
            _worldStateService.UpsertNpc(worldState, npc);
        }

        foreach (var mapObject in mapObjects)
        {
            _worldStateService.UpsertMapObject(worldState, mapObject);
        }

        _worldStateService.Touch(worldState, DateTime.UtcNow);
        return worldState;
    }

    private async Task ClearMongoCollectionsAsync(CancellationToken cancellationToken)
    {
        foreach (var mapping in DocumentMappingRegistry.All.Where(def => SeededEntityKeys.Contains(def.EntityKey)))
        {
            try
            {
                await _mongoDatabase.DropCollectionAsync(mapping.CollectionName, cancellationToken).ConfigureAwait(false);
                _logger.Info($"Dropped MongoDB collection {mapping.CollectionName} before seeding.");
            }
            catch (MongoCommandException ex) when (string.Equals(ex.CodeName, "NamespaceNotFound", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Debug($"MongoDB collection {mapping.CollectionName} did not exist prior to seeding.");
            }
        }
    }
}
