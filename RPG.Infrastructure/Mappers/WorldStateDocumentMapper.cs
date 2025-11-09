using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using RPG.Domain.Entities;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.MapObjects.MapObjectComponents;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Entities.Npcs.NpcComponents;
using RPG.Domain.Enums;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between WorldState domain entity and WorldStateDocument
/// </summary>
public class WorldStateDocumentMapper : IDocumentMapper<WorldState, WorldStateDocument>
{
    private readonly ILogger<WorldStateDocumentMapper> _logger;

    public WorldStateDocumentMapper(ILogger<WorldStateDocumentMapper> logger)
    {
        _logger = logger;
    }

    public WorldStateDocument ToDocument(WorldState entity)
    {
        _logger.Debug($"Converting WorldState to WorldStateDocument. Id={entity.Id}, WorldId={entity.WorldId}");

        return new WorldStateDocument
        {
            Id = entity.Id,
            WorldId = entity.WorldId,
            WorldName = entity.WorldName,
            LastUpdated = entity.LastUpdated,
            Characters = entity.Characters.Select(ToCharacterDocument).ToList(),
            Npcs = entity.Npcs.Select(ToNpcDocument).ToList(),
            MapObjects = entity.MapObjects.Select(ToMapObjectDocument).ToList()
        };
    }

    public WorldState ToDomain(WorldStateDocument document)
    {
        _logger.Debug($"Converting WorldStateDocument to WorldState. Id={document.Id}, WorldId={document.WorldId}");

        var characters = (document.Characters ?? new List<WorldCharacterStateDocument>())
            .Select(ToCharacter)
            .ToList();

        var npcs = (document.Npcs ?? new List<WorldNpcStateDocument>())
            .Select(ToNpc)
            .ToList();

        var mapObjects = (document.MapObjects ?? new List<WorldMapObjectStateDocument>())
            .Select(ToMapObject)
            .ToList();

        return WorldState.Hydrate(
            document.Id,
            document.WorldId,
            document.WorldName,
            document.LastUpdated,
            characters,
            npcs,
            mapObjects);
    }

    public WorldState ToEntity(WorldStateDocument document) => ToDomain(document);

    private static WorldCharacterStateDocument ToCharacterDocument(Character character)
    {
        return new WorldCharacterStateDocument
        {
            CharacterId = character.Id,
            SessionId = character.SessionId,
            DisplayName = character.Name,
            Location = ToLocationDocument(character.CurrentLocation),
            IsOnline = character.IsOnline,
            IsInCombat = character.IsInCombat,
            LastUpdated = character.LastUpdated,
            StatusEffects = new HashSet<string>(character.StatusEffects)
        };
    }

    private static WorldNpcStateDocument ToNpcDocument(Npc npc)
    {
        return new WorldNpcStateDocument
        {
            NpcId = npc.Id,
            Name = npc.Name,
            Location = ToLocationDocument(npc.CurrentLocation),
            IsAlive = npc.IsAlive,
            LastUpdated = npc.LastUpdated,
            RespawnAt = npc.RespawnAt,
            Tags = new HashSet<string>(npc.Tags)
        };
    }

    private static WorldMapObjectStateDocument ToMapObjectDocument(MapObject mapObject)
    {
        return new WorldMapObjectStateDocument
        {
            MapObjectId = mapObject.Id,
            Name = mapObject.Name,
            DisplayName = mapObject.DisplayName,
            Location = ToLocationDocument(mapObject.Location),
            IsActive = mapObject.IsActive,
            Tags = new HashSet<string>(mapObject.Tags),
            State = new Dictionary<string, string>(mapObject.State),
            LastUpdated = mapObject.LastUpdated
        };
    }

    private static Character ToCharacter(WorldCharacterStateDocument document)
    {
        var character = new Character(document.SessionId, CharacterClass.Warrior)
        {
            Id = document.CharacterId,
            Name = document.DisplayName,
            IsOnline = document.IsOnline,
            IsInCombat = document.IsInCombat,
            LastUpdated = document.LastUpdated,
            StatusEffects = document.StatusEffects is null
                ? new HashSet<string>()
                : new HashSet<string>(document.StatusEffects)
        };

        character.SetCurrentLocation(ToLocation(document.Location));
        return character;
    }

    private static Npc ToNpc(WorldNpcStateDocument document)
    {
        var location = ToLocation(document.Location);
        var worldId = location.WorldId ?? Guid.Empty;
        var npc = Npc.Create(document.Name, document.Name, CloneLocation(location), worldId,
            document.Tags is null ? new HashSet<string>() : new HashSet<string>(document.Tags));

        typeof(Npc).GetProperty("Id")!.SetValue(npc, document.NpcId);
        npc.SetCurrentLocation(location);
        npc.IsAlive = document.IsAlive;
        npc.LastUpdated = document.LastUpdated;
        npc.RespawnAt = document.RespawnAt;

        return npc;
    }

    private static MapObject ToMapObject(WorldMapObjectStateDocument document)
    {
        var location = ToLocation(document.Location);
        var worldId = location.WorldId ?? Guid.Empty;
        var mapObject = MapObject.Create(document.Name, location, worldId, location.ZoneName);

        typeof(MapObject).GetProperty("Id")!.SetValue(mapObject, document.MapObjectId);
        mapObject.DisplayName = document.DisplayName;
        mapObject.IsActive = document.IsActive;
        mapObject.Tags = document.Tags is null
            ? new HashSet<string>()
            : new HashSet<string>(document.Tags);
        mapObject.State = document.State is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(document.State);
        mapObject.LastUpdated = document.LastUpdated;

        return mapObject;
    }

    private static WorldLocationDocument ToLocationDocument(Location location)
    {
        var safeLocation = location ?? new Location();
        var position = safeLocation.Position;
        return new WorldLocationDocument
        {
            X = position.X,
            Y = position.Y,
            Z = position.Z,
            WorldId = safeLocation.WorldId,
            MapId = safeLocation.MapId ?? string.Empty,
            ZoneName = safeLocation.ZoneName ?? string.Empty,
            Rotation = safeLocation.Rotation
        };
    }

    private static Location ToLocation(WorldLocationDocument? document)
    {
        if (document is null)
        {
            return new Location();
        }

        var location = new Location
        {
            WorldId = document.WorldId,
            MapId = document.MapId ?? string.Empty,
            ZoneName = document.ZoneName ?? string.Empty,
            Rotation = document.Rotation
        };

        location.Position = new Vector3((float)document.X, (float)document.Y, (float)document.Z);
        return location;
    }

    private static Location CloneLocation(Location location)
    {
        var clone = new Location
        {
            WorldId = location.WorldId,
            MapId = location.MapId,
            ZoneName = location.ZoneName,
            Rotation = location.Rotation
        };

        clone.Position = location.Position;
        return clone;
    }
}
