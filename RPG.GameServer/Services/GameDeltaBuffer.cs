using System.Collections.Concurrent;
using RPG.Abstractions.SharedModel;
using Protos = RPG.GameServer.Protos;

namespace RPG.GameServer.Services;

/// <summary>
/// Buforuje delty GameDeltaUpdate per world, aby można było wysyłać je batchami w StreamWorldState.
/// </summary>
public sealed class GameDeltaBuffer
{
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<GameDeltaUpdate>> _worldQueues = new();
    private readonly RPG.Infrastructure.Interfaces.ILogger<GameDeltaBuffer> _logger;

    public GameDeltaBuffer(RPG.Infrastructure.Interfaces.ILogger<GameDeltaBuffer> logger)
    {
        _logger = logger;
    }

    public void Enqueue(GameDeltaUpdate delta)
    {
        if (delta == null)
        {
            return;
        }

        var queue = _worldQueues.GetOrAdd(delta.WorldId, _ => new ConcurrentQueue<GameDeltaUpdate>());
        queue.Enqueue(delta);
    }

    /// <summary>
    /// Pobiera i czyści wszystkie delty dla danego worldId, agregując je do pojedynczego WorldDelta.
    /// </summary>
    public Protos.WorldDelta DequeueAggregated(Guid worldId)
    {
        var result = new Protos.WorldDelta();

        if (!_worldQueues.TryGetValue(worldId, out var queue))
        {
            return result;
        }

        var npcList = new List<Protos.WorldNpcDelta>();
        var characterList = new List<Protos.WorldCharacterDelta>();
        var mapObjectList = new List<Protos.WorldMapObjectDelta>();

        while (queue.TryDequeue(out var delta))
        {
            foreach (var npc in delta.NpcChanges)
            {
                npcList.Add(new Protos.WorldNpcDelta
                {
                    NpcId = npc.NpcId.ToString(),
                    Location = npc.Location is null ? null : new Protos.Location
                    {
                        X = npc.Location.Position.X,
                        Y = npc.Location.Position.Y,
                        Z = npc.Location.Position.Z,
                        WorldId = npc.Location.WorldId.ToString(),
                        MapId = npc.Location.MapId ?? string.Empty,
                        ZoneName = npc.Location.MapName ?? string.Empty,
                        Rotation = npc.Location.Direction
                    }
                });
            }

            foreach (var ch in delta.CharacterChanges)
            {
                characterList.Add(new Protos.WorldCharacterDelta
                {
                    CharacterId = ch.CharacterId.ToString(),
                    Location = ch.Location is null ? null : new Protos.Location
                    {
                        X = ch.Location.Position.X,
                        Y = ch.Location.Position.Y,
                        Z = ch.Location.Position.Z,
                        WorldId = ch.Location.WorldId.ToString(),
                        MapId = ch.Location.MapId ?? string.Empty,
                        ZoneName = ch.Location.MapName ?? string.Empty,
                        Rotation = ch.Location.Direction
                    }
                });
            }

            foreach (var mapObject in delta.MapObjectChanges)
            {
                mapObjectList.Add(new Protos.WorldMapObjectDelta
                {
                    MapObjectId = mapObject.MapObjectId.ToString(),
                    Location = mapObject.Location is null ? null : new Protos.Location
                    {
                        X = mapObject.Location.Position.X,
                        Y = mapObject.Location.Position.Y,
                        Z = mapObject.Location.Position.Z,
                        WorldId = mapObject.Location.WorldId.ToString(),
                        MapId = mapObject.Location.MapId ?? string.Empty,
                        ZoneName = mapObject.Location.MapName ?? string.Empty,
                        Rotation = mapObject.Location.Direction
                    },
                    IsActive = mapObject.IsActive ?? false
                });
            }
        }

        result.Npcs.AddRange(npcList);
        result.Characters.AddRange(characterList);
        result.MapObjects.AddRange(mapObjectList);
        return result;
    }
}
