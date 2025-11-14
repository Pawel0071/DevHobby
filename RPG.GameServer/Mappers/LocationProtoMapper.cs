using RPG.GameServer.QueryProtos;
using DomainLocation = RPG.Domain.Models.Location;
using ProtoLocation = RPG.GameServer.QueryProtos.Location;

namespace RPG.GameServer.Mappers;

/// <summary>
/// Mapper for Location domain model to proto message
/// </summary>
public class LocationProtoMapper
{
    public ProtoLocation ToProto(DomainLocation location)
    {
        return new ProtoLocation
        {
            X = location.Position.X,
            Y = location.Position.Y,
            Z = location.Position.Z,
            WorldId = location.WorldId?.ToString() ?? string.Empty,
            MapId = location.MapId ?? string.Empty,
            ZoneName = location.ZoneName ?? string.Empty,
            Rotation = location.Rotation
        };
    }

    public DomainLocation ToDomain(ProtoLocation proto)
    {
        var worldId = Guid.TryParse(proto.WorldId, out var wId) ? wId : Guid.Empty;
        var location = DomainLocation.Create(
            (float)proto.X,
            (float)proto.Y,
            (float)proto.Z,
            worldId,
            proto.MapId,
            proto.ZoneName
        );
        location.Rotation = proto.Rotation;
        return location;
    }
}

