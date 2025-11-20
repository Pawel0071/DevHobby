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
            WorldId = location.WorldId.ToString(),
            MapId = location.MapId ?? string.Empty,
            Position = new Vector3
            {
                X = location.Position.X,
                Y = location.Position.Y,
                Z = location.Position.Z
            },
            Rotation = new RotationState
            {
                IsRotating = location.Direction != 0,
                Direction = location.Direction
            }
        };
    }

    public DomainLocation ToDomain(ProtoLocation proto)
    {
        var worldId = Guid.TryParse(proto.WorldId, out var wId) ? wId : Guid.Empty;
        var x = proto.Position?.X ?? 0;
        var y = proto.Position?.Y ?? 0;
        var z = proto.Position?.Z ?? 0;
        var location = DomainLocation.Create((float)x, (float)y, (float)z, worldId, proto.MapId, string.Empty);
        location.Direction = proto.Rotation?.Direction ?? 0;
        return location;
    }
}
