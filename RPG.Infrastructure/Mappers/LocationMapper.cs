using System.Numerics;
using RPG.Domain.Entities;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between Location domain entity and LocationData document
/// </summary>
public class LocationMapper
{
    private readonly ILogger<LocationMapper> _logger;

    public LocationMapper(ILogger<LocationMapper> logger)
    {
        _logger = logger;
    }

    public LocationData ToDocument(Location entity)
    {
        _logger.Debug($"Converting Location to LocationData. Position={entity.Position}, WorldId={entity.WorldId}");
        return new LocationData
        {
            X = entity.Position.X,
            Y = entity.Position.Y,
            Z = entity.Position.Z,
            WorldId = entity.WorldId?.ToString(),
            MapId = entity.MapId,
            ZoneName = entity.ZoneName,
            Rotation = entity.Rotation
        };
    }

    public Location ToEntity(LocationData document)
    {
        _logger.Debug($"Converting LocationData to Location. X={document.X}, Y={document.Y}, Z={document.Z}");
        var worldId = string.IsNullOrEmpty(document.WorldId)
            ? (Guid?)null
            : Guid.Parse(document.WorldId);

        var location = Location.Create(
            new Vector3(document.X, document.Y, document.Z),
            worldId ?? Guid.Empty,
            document.MapId,
            document.ZoneName);

        location.Rotation = document.Rotation;
        location.WorldId = worldId;

        return location;
    }
}