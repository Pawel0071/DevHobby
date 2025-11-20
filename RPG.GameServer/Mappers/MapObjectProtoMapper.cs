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
                    ItemName = slot.Item?.Name ?? string.Empty,
                    Quantity = slot.Quantity
                });
            }
            proto.Container = c;
            // JSON mirror wpisu dla testów integracyjnych (typed + generic)
            proto.Components.Add(new Component
            {
                Type = nameof(ContainerComponent),
                DataJson = JsonSerializer.Serialize(container, typeof(ContainerComponent))
            });
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
            proto.Components.Add(new Component
            {
                Type = nameof(LockableComponent),
                DataJson = JsonSerializer.Serialize(lockable, typeof(LockableComponent))
            });
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
            proto.Components.Add(new Component
            {
                Type = nameof(DoorComponent),
                DataJson = JsonSerializer.Serialize(door, typeof(DoorComponent))
            });
        }

        // Add remaining components as generic serialized components, skipping the explicitly typed ones above
        foreach (var component in domain.Components)
        {
            if (component is ContainerComponent || component is LockableComponent || component is DoorComponent)
                continue;

            proto.Components.Add(new Component
            {
                Type = component.GetType().Name,
                DataJson = JsonSerializer.Serialize(component, component.GetType())
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
        var location = proto.Location is not null ? _locationMapper.ToDomain(proto.Location) : RPG.Domain.Models.Location.Create(0, 0, 0, worldId);

        var mapObject = DomainMapObject.Create(
            proto.Name,
            location,
            worldId,
            proto.ZoneId
        );

        // Do not override Id (no reflection allowed) – keep Id generated by the factory
        mapObject.RotationYaw = proto.RotationYaw;
        mapObject.IsActive = proto.IsActive;
        mapObject.LastUpdated = DateTimeOffset.FromUnixTimeMilliseconds(proto.LastUpdatedUnixMs).UtcDateTime;

        // Sync additional basic fields
        mapObject.DisplayName = string.IsNullOrWhiteSpace(proto.DisplayName) ? proto.Name : proto.DisplayName;
        mapObject.Description = proto.Description ?? string.Empty;

        foreach (var tag in proto.Tags)
        {
            mapObject.Tags.Add(tag);
        }

        foreach (var kv in proto.State)
        {
            mapObject.State[kv.Key] = kv.Value;
        }

        // Components (typed ones)
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

        // Components (generic, serialized) – skipping without reflection; log and ignore
        if (proto.Components is not null)
        {
            foreach (var c in proto.Components)
            {
                // Skip ones handled explicitly above
                if (string.Equals(c.Type, nameof(ContainerComponent), StringComparison.Ordinal) ||
                    string.Equals(c.Type, nameof(LockableComponent), StringComparison.Ordinal) ||
                    string.Equals(c.Type, nameof(DoorComponent), StringComparison.Ordinal))
                {
                    continue;
                }

                _logger.Warn($"Skipping generic component '{c.Type}' during ToDomain – reflection disabled.");
            }
        }

        _logger.Debug($"MapObject domain created. Id={mapObject.Id}");
        return mapObject;
    }
}
