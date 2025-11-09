using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Logging;
using RPG.Core.Interfaces;
using RPG.Domain.Entities;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.Npcs;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.WorldSeeder.Services;

internal sealed class WorldSeederService
{
    private const int WorldWidth = 50;
    private const int WorldHeight = 30;
    private const string MapId = "starter.map";
    private const string ZoneName = "starter.zone";
    private static readonly Guid StarterWorldId = Guid.Parse("c2bce5a0-5d6d-4eb5-8f5c-5aeb1b6f6b3d");

    private readonly IMongoDocumentRepository _mongoRepository;
    private readonly IDocumentMapper<WorldState, WorldStateDocument> _worldMapper;
    private readonly IWorldStateService _worldStateService;
    private readonly Microsoft.Extensions.Logging.ILogger<WorldSeederService> _logger;

    public WorldSeederService(
        IMongoDocumentRepository mongoRepository,
        IDocumentMapper<WorldState, WorldStateDocument> worldMapper,
        IWorldStateService worldStateService,
    Microsoft.Extensions.Logging.ILogger<WorldSeederService> logger)
    {
        _mongoRepository = mongoRepository;
        _worldMapper = worldMapper;
        _worldStateService = worldStateService;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding world definition for {WorldName} ({WorldId})", "Starter Grounds", StarterWorldId);

        var existingWorlds = await _mongoRepository.GetAllAsync<WorldStateDocument>(cancellationToken);
        var existing = existingWorlds.FirstOrDefault(w => w.WorldId == StarterWorldId);

        var persistentId = existing?.Id ?? Guid.NewGuid();
        var now = DateTime.UtcNow;

        var world = WorldState.Hydrate(persistentId, StarterWorldId, "Starter Grounds", now);

        SeedNpcs(world, now);
        SeedMapObjects(world, now);

        _worldStateService.Touch(world, now);

        var document = _worldMapper.ToDocument(world);
        await _mongoRepository.UpsertAsync(document, cancellationToken);

        _logger.LogInformation("World {WorldId} persisted with {NpcCount} NPCs and {ObjectCount} map objects.", StarterWorldId, world.Npcs.Count, world.MapObjects.Count);
    }

    private void SeedNpcs(WorldState world, DateTime timestamp)
    {
        var guideLocation = CreateLocation(12, 8, 180f);
        var friendlyNpc = Npc.Create("starter.npc.village-guide", "Village Guide", CloneLocation(guideLocation), StarterWorldId, new HashSet<string> { "friendly", "guide", "questgiver" });
        typeof(Npc).GetProperty("Id")!.SetValue(friendlyNpc, Guid.Parse("6fd699a7-2ebb-4b31-8b16-603bcb35c1a4"));
        friendlyNpc.SetCurrentLocation(guideLocation);
        friendlyNpc.IsAlive = true;
        friendlyNpc.LastUpdated = timestamp;
        friendlyNpc.RespawnAt = null;

        var scoutLocation = CreateLocation(34, 12, 90f);
        var hostileScout = Npc.Create("starter.npc.goblin-scout", "Goblin Scout", CloneLocation(scoutLocation), StarterWorldId, new HashSet<string> { "enemy", "goblin", "scout" });
        typeof(Npc).GetProperty("Id")!.SetValue(hostileScout, Guid.Parse("49f0ce44-6e37-4d80-9f33-a8a894fe6a77"));
        hostileScout.SetCurrentLocation(scoutLocation);
        hostileScout.IsAlive = true;
        hostileScout.LastUpdated = timestamp;
        hostileScout.RespawnAt = timestamp.AddMinutes(3);

        var warriorLocation = CreateLocation(40, 20, 270f);
        var hostileWarrior = Npc.Create("starter.npc.goblin-warrior", "Goblin Warrior", CloneLocation(warriorLocation), StarterWorldId, new HashSet<string> { "enemy", "goblin", "melee" });
        typeof(Npc).GetProperty("Id")!.SetValue(hostileWarrior, Guid.Parse("3a07884f-2f3c-49fb-b7a9-449e6bd01958"));
        hostileWarrior.SetCurrentLocation(warriorLocation);
        hostileWarrior.IsAlive = true;
        hostileWarrior.LastUpdated = timestamp;
        hostileWarrior.RespawnAt = timestamp.AddMinutes(5);

        _worldStateService.UpsertNpc(world, friendlyNpc);
        _worldStateService.UpsertNpc(world, hostileScout);
        _worldStateService.UpsertNpc(world, hostileWarrior);
    }

    private void SeedMapObjects(WorldState world, DateTime timestamp)
    {
        var oakTree = MapObject.Create("starter.tree.oak", CreateLocation(8, 5, 0f), StarterWorldId, ZoneName);
        typeof(MapObject).GetProperty("Id")!.SetValue(oakTree, Guid.Parse("b7c9cecb-4d93-4ee0-8a86-e363df3ae73c"));
        oakTree.DisplayName = "Ancient Oak";
        oakTree.IsActive = true;
        oakTree.LastUpdated = timestamp;
        oakTree.Tags = new HashSet<string> { "tree", "scenery" };
        oakTree.State = new Dictionary<string, string>
        {
            ["species"] = "oak",
            ["height"] = "12"
        };

        var pineTree = MapObject.Create("starter.tree.pine", CreateLocation(42, 6, 45f), StarterWorldId, ZoneName);
        typeof(MapObject).GetProperty("Id")!.SetValue(pineTree, Guid.Parse("f2ddbaea-35c4-4af5-9e6e-00722ed9951b"));
        pineTree.DisplayName = "Tall Pine";
        pineTree.IsActive = true;
        pineTree.LastUpdated = timestamp;
        pineTree.Tags = new HashSet<string> { "tree", "scenery" };
        pineTree.State = new Dictionary<string, string>
        {
            ["species"] = "pine",
            ["height"] = "15"
        };

        var townHall = MapObject.Create("starter.building.townhall", CreateLocation(18, 10, 180f), StarterWorldId, ZoneName);
        typeof(MapObject).GetProperty("Id")!.SetValue(townHall, Guid.Parse("0cb43a3f-bd3c-4dc6-8781-f1024a816569"));
        townHall.DisplayName = "Town Hall";
        townHall.IsActive = true;
        townHall.LastUpdated = timestamp;
        townHall.Tags = new HashSet<string> { "building", "hub" };
        townHall.State = new Dictionary<string, string>
        {
            ["worldWidth"] = WorldWidth.ToString(CultureInfo.InvariantCulture),
            ["worldHeight"] = WorldHeight.ToString(CultureInfo.InvariantCulture),
            ["floors"] = "2"
        };

        var treasureChest = MapObject.Create("starter.chest.community", CreateLocation(25, 15, 0f), StarterWorldId, ZoneName);
        typeof(MapObject).GetProperty("Id")!.SetValue(treasureChest, Guid.Parse("a3aa4c7d-2d4d-4b0c-9f4d-2fe62f0d45ed"));
        treasureChest.DisplayName = "Community Chest";
        treasureChest.IsActive = true;
        treasureChest.LastUpdated = timestamp;
        treasureChest.Tags = new HashSet<string> { "chest", "interactive" };
        treasureChest.State = new Dictionary<string, string>
        {
            ["lockState"] = "unlocked",
            ["lootTable"] = "starter.community"
        };

        _worldStateService.UpsertMapObject(world, oakTree);
        _worldStateService.UpsertMapObject(world, pineTree);
        _worldStateService.UpsertMapObject(world, townHall);
        _worldStateService.UpsertMapObject(world, treasureChest);
    }

    private static Location CreateLocation(float x, float y, float rotationDegrees)
    {
        var location = Location.Create(new Vector3(x, y, 0f), StarterWorldId, MapId, ZoneName);
        location.Rotation = rotationDegrees;
        return location;
    }

    private static Location CloneLocation(Location location)
    {
        var clone = Location.Create(location.Position, location.WorldId ?? StarterWorldId, location.MapId, location.ZoneName);
        clone.Rotation = location.Rotation;
        return clone;
    }
}
