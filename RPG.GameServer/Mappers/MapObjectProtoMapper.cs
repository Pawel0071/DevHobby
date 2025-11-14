using RPG.GameServer.QueryProtos;
using DomainMapObject = RPG.Domain.Models.MapObjects.MapObject;
using RPG.Domain.Models.MapObjects.MapObjectComponents;
using System.Text.Json;

namespace RPG.GameServer.Mappers;

/// <summary>
/// Mapper for MapObject domain model to proto message
/// </summary>
public class MapObjectProtoMapper : IProtoMapper<DomainMapObject, MapObject>
{
    private readonly RPG.Infrastructure.Interfaces.ILogger<MapObjectProtoMapper> _logger;
    private readonly LocationProtoMapper _locationMapper;

    public MapObjectProtoMapper(
        RPG.Infrastructure.Interfaces.ILogger<MapObjectProtoMapper> logger,
        LocationProtoMapper locationMapper)
    {
        _logger = logger;
        _locationMapper = locationMapper;
    }

    public MapObject ToProto(DomainMapObject domain)
    {
        _logger.Debug($"Converting MapObject to proto. Id={domain.Id}, Name={domain.Name}");

        var proto = new MapObject
        {
            Id = domain.Id.ToString(),
            Name = domain.Name,
            DisplayName = domain.DisplayName,
            Description = domain.Description,
            TypeCode = string.Empty, // TypeCode is not in domain model
            Location = _locationMapper.ToProto(domain.Location),
            RotationYaw = domain.RotationYaw,
            WorldId = domain.WorldId.ToString(),
            ZoneId = domain.ZoneId,
            IsActive = domain.IsActive,
            LastUpdatedUnixMs = new DateTimeOffset(domain.LastUpdated).ToUnixTimeMilliseconds()
        };

        proto.Tags.AddRange(domain.Tags);

        foreach (var kv in domain.State ?? new Dictionary<string, string>())
        {
            proto.State[kv.Key] = kv.Value;
        }

        // Components
        if (domain.GetComponent<ContainerComponent>() is { } container)
        {
            var c = new ContainerComponentTyped();
            foreach (var slot in container.Items)
            {
                c.Items.Add(new InventorySlot
                {
                    ItemId = slot.Item?.Id.ToString() ?? string.Empty,
                    Quantity = slot.Quantity
                });
            }
            proto.Container = c;
        }

        if (domain.GetComponent<LockableComponent>() is { } lockable)
        {
            proto.Lockable = new LockableComponentTyped
            {
                IsLocked = lockable.IsLocked,
                RequiredKeyItemId = lockable.RequiredKeyItemId ?? string.Empty,
                LockpickDifficulty = lockable.LockpickDifficulty,
                CanBeLockpicked = lockable.CanBeLockpicked
            };
        }

        if (domain.GetComponent<DoorComponent>() is { } door)
        {
            proto.Door = new DoorComponentTyped
            {
                IsOpen = door.IsOpen,
                LinkedDoorId = door.LinkedDoorId?.ToString() ?? string.Empty,
                OpenAnimation = door.OpenAnimation ?? string.Empty,
                CloseAnimation = door.CloseAnimation ?? string.Empty,
                OpenAngle = door.OpenAngle,
                AutoClose = door.AutoClose,
                AutoCloseDelaySeconds = door.AutoCloseDelaySeconds
            };
        }

        foreach (var component in domain.Components)
        {
            proto.Components.Add(new Component
            {
                Type = component.GetType().Name,
                Data = JsonSerializer.Serialize(component, component.GetType())
            });
        }

        _logger.Debug($"MapObject proto created. Id={proto.Id}");
        return proto;
    }

    public DomainMapObject ToDomain(MapObject proto)
    {
        _logger.Debug($"Converting MapObject proto to domain. Id={proto.Id}, Name={proto.Name}");

        var id = Guid.TryParse(proto.Id, out var parsed) ? parsed : Guid.NewGuid();
        var worldId = Guid.TryParse(proto.WorldId, out var wId) ? wId : Guid.NewGuid();
        var location = _locationMapper.ToDomain(proto.Location);

        var mapObject = DomainMapObject.Create(
            proto.Name,
            location,
            worldId,
            proto.ZoneId
        );

        // Override Id
        typeof(DomainMapObject).GetProperty(nameof(DomainMapObject.Id))?.SetValue(mapObject, id);
        mapObject.RotationYaw = proto.RotationYaw;
        mapObject.IsActive = proto.IsActive;
        mapObject.LastUpdated = DateTimeOffset.FromUnixTimeMilliseconds(proto.LastUpdatedUnixMs).UtcDateTime;

        foreach (var tag in proto.Tags)
        {
            mapObject.Tags.Add(tag);
        }

        foreach (var kv in proto.State)
        {
            mapObject.State[kv.Key] = kv.Value;
        }

        // Components
        if (proto.Container is not null)
        {
            // ContainerComponent.Items is readonly - we need IItemRepository to resolve items
            // For now, create empty container - full implementation needs item resolution
            var component = new ContainerComponent();
            mapObject.Components.Add(component);
        }

        if (proto.Lockable is not null)
        {
            mapObject.Components.Add(new LockableComponent
            {
                IsLocked = proto.Lockable.IsLocked,
                RequiredKeyItemId = proto.Lockable.RequiredKeyItemId,
                LockpickDifficulty = proto.Lockable.LockpickDifficulty,
                CanBeLockpicked = proto.Lockable.CanBeLockpicked
            });
        }

        if (proto.Door is not null)
        {
            var doorId = Guid.TryParse(proto.Door.LinkedDoorId, out var dId) ? dId : (Guid?)null;
            mapObject.Components.Add(new DoorComponent
            {
                IsOpen = proto.Door.IsOpen,
                LinkedDoorId = doorId,
                OpenAnimation = proto.Door.OpenAnimation,
                CloseAnimation = proto.Door.CloseAnimation,
                OpenAngle = proto.Door.OpenAngle,
                AutoClose = proto.Door.AutoClose,
                AutoCloseDelaySeconds = proto.Door.AutoCloseDelaySeconds
            });
        }

        _logger.Debug($"MapObject domain created. Id={mapObject.Id}");
        return mapObject;
    }
}
