using RPG.Domain.Entities;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between WorldState domain entity and WorldStateDocument
/// </summary>
public class WorldStateDocumentMapper
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
            Id = entity.Id, WorldId = entity.WorldId, WorldName = entity.WorldName, LastUpdated = entity.LastUpdated
        };
    }

    public WorldState ToEntity(WorldStateDocument document)
    {
        _logger.Debug($"Converting WorldStateDocument to WorldState. Id={document.Id}, WorldId={document.WorldId}");
        var worldState = WorldState.Create(document.WorldId, document.WorldName);

        // Update fields using reflection since Create() sets defaults
        typeof(WorldState).GetProperty("Id")!.SetValue(worldState, document.Id);
        worldState.LastUpdated = document.LastUpdated;

        return worldState;
    }
}
