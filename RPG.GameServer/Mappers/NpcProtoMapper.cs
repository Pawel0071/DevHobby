using RPG.GameServer.QueryProtos;
using DomainNpc = RPG.Domain.Models.Npcs.Npc;

namespace RPG.GameServer.Mappers;

/// <summary>
/// Mapper for Npc domain model to proto message
/// </summary>
public class NpcProtoMapper : IProtoMapper<DomainNpc, Npc>
{
    private readonly RPG.Infrastructure.Interfaces.ILogger<NpcProtoMapper> _logger;
    private readonly LocationProtoMapper _locationMapper;

    public NpcProtoMapper(
        RPG.Infrastructure.Interfaces.ILogger<NpcProtoMapper> logger,
        LocationProtoMapper locationMapper)
    {
        _logger = logger;
        _locationMapper = locationMapper;
    }

    public Npc ToProto(DomainNpc domain)
    {
        _logger.Debug($"Converting Npc to proto. Id={domain.Id}, Name={domain.Name}");

        var proto = new Npc
        {
            Id = domain.Id.ToString(),
            Name = domain.Name,
            Level = domain.Level,
            IsMoving = domain.IsMoving,
            X = domain.CurrentLocation.Position.X,
            Y = domain.CurrentLocation.Position.Y,
            Z = domain.CurrentLocation.Position.Z,
            Rotation = domain.CurrentLocation.Rotation
        };

        proto.Tags.AddRange(domain.Tags);

        _logger.Debug($"Npc proto created. Id={proto.Id}");
        return proto;
    }

    public DomainNpc ToDomain(Npc proto)
    {
        _logger.Debug($"Converting Npc proto to domain. Id={proto.Id}, Name={proto.Name}");

        var id = Guid.TryParse(proto.Id, out var parsed) ? parsed : Guid.NewGuid();
        var worldId = Guid.NewGuid(); // WorldId needs to be provided from context
        var location = RPG.Domain.Models.Location.Create(proto.X, proto.Y, proto.Z, worldId);
        location.Rotation = proto.Rotation;

        var npc = DomainNpc.Create(proto.Name, string.Empty, location, worldId);

        // Override Id
        typeof(DomainNpc).GetProperty(nameof(DomainNpc.Id))?.SetValue(npc, id);
        npc.Level = proto.Level;
        npc.SetMovementState(proto.IsMoving);

        foreach (var tag in proto.Tags)
        {
            npc.Tags.Add(tag);
        }

        _logger.Debug($"Npc domain created. Id={npc.Id}");
        return npc;
    }
}

