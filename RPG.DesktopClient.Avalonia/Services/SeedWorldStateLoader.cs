using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RPG.GameServer.Protos;

namespace RPG.DesktopClient.Avalonia.Services;

internal sealed class SeedWorldStateLoader
{
    private readonly string _seedRoot;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public SeedWorldStateLoader(IConfiguration configuration)
    {
        _seedRoot = ResolveSeedRoot(configuration);
    }

    public async Task<WorldSnapshot?> TryLoadAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_seedRoot))
        {
            return null;
        }

        var worldStateFolder = Path.Combine(_seedRoot, "WorldState");
        if (!Directory.Exists(worldStateFolder))
        {
            return null;
        }

        var worldStateFile = Directory
            .EnumerateFiles(worldStateFolder, "*.json", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();
        if (worldStateFile is null)
        {
            return null;
        }

        var worldState = await DeserializeAsync<WorldStateSeedModel>(worldStateFile, cancellationToken).ConfigureAwait(false);
        if (worldState is null)
        {
            return null;
        }

        var npcDirectory = Path.Combine(_seedRoot, "Npcs");
        var mapObjectDirectory = Path.Combine(_seedRoot, "MapObjects");

        var allowedNpcIds = worldState.Npcs != null
            ? new HashSet<Guid>(worldState.Npcs)
            : new HashSet<Guid>();
        var allowedMapObjectIds = worldState.MapObjects != null
            ? new HashSet<Guid>(worldState.MapObjects)
            : new HashSet<Guid>();

        var snapshot = new WorldSnapshot
        {
            Metadata = new WorldMetadata
            {
                WorldId = worldState.WorldId != Guid.Empty ? worldState.WorldId.ToString() : string.Empty,
                WorldName = string.IsNullOrWhiteSpace(worldState.WorldName)
                    ? "Seeded World"
                    : worldState.WorldName
            },
            LastUpdated = worldState.LastUpdated.HasValue
                ? new DateTimeOffset(worldState.LastUpdated.Value).ToUnixTimeMilliseconds()
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        if (Directory.Exists(npcDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(npcDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var model = await DeserializeAsync<NpcSeedModel>(file, cancellationToken).ConfigureAwait(false);
                if (model is null)
                {
                    continue;
                }

                if (allowedNpcIds.Count > 0 && !allowedNpcIds.Contains(model.Id))
                {
                    continue;
                }

                var locationModel = model.CurrentLocation ?? model.SpawnLocation;
                if (locationModel is null)
                {
                    continue;
                }

                var location = ToProtoLocation(locationModel);
                var npc = new WorldNpc
                {
                    NpcId = model.Id.ToString(),
                    Name = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Name : model.DisplayName,
                    Location = location,
                    IsAlive = model.IsAlive,
                    LastUpdated = model.LastUpdated.HasValue
                        ? new DateTimeOffset(model.LastUpdated.Value).ToUnixTimeMilliseconds()
                        : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    RespawnAt = model.RespawnAt.HasValue
                        ? new DateTimeOffset(model.RespawnAt.Value).ToUnixTimeMilliseconds()
                        : 0
                };

                if (model.Tags != null)
                {
                    npc.Tags.AddRange(model.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
                }

                snapshot.Npcs.Add(npc);
            }
        }

        if (Directory.Exists(mapObjectDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(mapObjectDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var model = await DeserializeAsync<MapObjectSeedModel>(file, cancellationToken).ConfigureAwait(false);
                if (model is null)
                {
                    continue;
                }

                if (allowedMapObjectIds.Count > 0 && !allowedMapObjectIds.Contains(model.Id))
                {
                    continue;
                }

                if (model.Location is null)
                {
                    continue;
                }

                var mapObject = new WorldMapObject
                {
                    MapObjectId = model.Id.ToString(),
                    Name = model.Name,
                    DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Name : model.DisplayName,
                    Location = ToProtoLocation(model.Location),
                    IsActive = model.IsActive,
                    LastUpdated = model.LastUpdated.HasValue
                        ? new DateTimeOffset(model.LastUpdated.Value).ToUnixTimeMilliseconds()
                        : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                if (model.Tags != null)
                {
                    mapObject.Tags.AddRange(model.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
                }

                if (model.State != null)
                {
                    foreach (var kvp in model.State)
                    {
                        mapObject.State[kvp.Key] = kvp.Value;
                    }
                }

                snapshot.MapObjects.Add(mapObject);
            }
        }

        return snapshot;
    }

    private async Task<T?> DeserializeAsync<T>(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private static Location ToProtoLocation(LocationSeedModel model)
    {
        var location = new Location
        {
            X = model.Position?.X ?? 0f,
            Y = model.Position?.Y ?? 0f,
            Z = model.Position?.Z ?? 0f,
            Rotation = model.Rotation,
            MapId = model.MapId ?? string.Empty,
            ZoneName = model.ZoneName ?? string.Empty,
            WorldId = model.WorldId?.ToString() ?? string.Empty
        };

        return location;
    }

    private static string ResolveSeedRoot(IConfiguration configuration)
    {
        var configuredPath = configuration.GetValue<string>("SeedData:Path")
                              ?? Environment.GetEnvironmentVariable("RPG_SEEDDATA_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath) && Directory.Exists(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var basePath = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(basePath, "SeedData"),
            Path.Combine(basePath, "..", "..", "..", "..", "RPG.WorldSeeder", "SeedData"),
            Path.Combine(basePath, "..", "..", "..", "..", "..", "RPG.WorldSeeder", "SeedData"),
            Path.Combine(basePath, "..", "..", "RPG.WorldSeeder", "SeedData")
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

    return basePath;
    }

    private sealed class WorldStateSeedModel
    {
        public Guid Id { get; init; }
        public Guid WorldId { get; init; }
        public string WorldName { get; init; } = string.Empty;
        public DateTime? LastUpdated { get; init; }
        public List<Guid>? Npcs { get; init; }
        public List<Guid>? MapObjects { get; init; }
    }

    private sealed class NpcSeedModel
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public bool IsAlive { get; init; } = true;
        public DateTime? LastUpdated { get; init; }
        public DateTime? RespawnAt { get; init; }
        public List<string>? Tags { get; init; }
        public LocationSeedModel? CurrentLocation { get; init; }
        public LocationSeedModel SpawnLocation { get; init; } = new();
    }

    private sealed class MapObjectSeedModel
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? DisplayName { get; init; }
        public LocationSeedModel Location { get; init; } = new();
        public bool IsActive { get; init; } = true;
        public DateTime? LastUpdated { get; init; }
        public List<string>? Tags { get; init; }
        public Dictionary<string, string>? State { get; init; }
    }

    private sealed class LocationSeedModel
    {
        public Guid? WorldId { get; init; }
        public string? MapId { get; init; }
        public string? ZoneName { get; init; }
        public float Rotation { get; init; }
        public PositionSeedModel? Position { get; init; }
    }

    private sealed class PositionSeedModel
    {
        public float X { get; init; }
        public float Y { get; init; }
        public float Z { get; init; }
    }
}
