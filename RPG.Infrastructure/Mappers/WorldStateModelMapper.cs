using System;
using System.Linq;
using RPG.Domain.Entities;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between WorldState domain entity and WorldStateDocument
/// </summary>
public class WorldStateModelMapper : IModelMapper<WorldState, WorldStateDocument>
{
    private readonly ILogger<WorldStateModelMapper> _logger;

    public WorldStateModelMapper(ILogger<WorldStateModelMapper> logger)
    {
        _logger = logger;
    }

    public WorldStateDocument ToPersistence(WorldState entity)
    {
        _logger.Debug($"Converting WorldState to WorldStateDocument. Id={entity.Id}, WorldId={entity.WorldId}");

        return new WorldStateDocument
        {
            Id = entity.Id,
            WorldId = entity.WorldId,
            WorldName = entity.WorldName,
            LastUpdated = entity.LastUpdated,
            Characters = entity.Characters.ToList(),
            Npcs = entity.Npcs.ToList(),
            MapObjects = entity.MapObjects.ToList()
        };
    }

    public WorldState ToDomain(WorldStateDocument document)
    {
        _logger.Debug($"Converting WorldStateDocument to WorldState. Id={document.Id}, WorldId={document.WorldId}");

        return WorldState.Hydrate(
            document.Id,
            document.WorldId,
            document.WorldName,
            document.LastUpdated,
            document.Characters ?? Enumerable.Empty<Guid>(),
            document.Npcs ?? Enumerable.Empty<Guid>(),
            document.MapObjects ?? Enumerable.Empty<Guid>());
    }

    public WorldState ToEntity(WorldStateDocument document) => ToDomain(document);
}
