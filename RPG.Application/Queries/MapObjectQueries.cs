// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Queries/MapObjectQueries.cs
using RPG.Application.Interfaces;
using RPG.Domain.Models.MapObjects;
using RPG.Domain.Models.MapObjects.MapObjectComponents;
using RPG.Infrastructure.Interfaces;
using System.Text.Json;

namespace RPG.Application.Queries;

public sealed record GetMapObjectQuery(Guid MapObjectId) : IQuery<MapObjectReadDto>;
public sealed record GetMapObjectsQuery() : IQuery<IReadOnlyList<MapObjectReadDto>>;
public sealed record GetMapObjectsByIdsQuery(IReadOnlyCollection<Guid> MapObjectIds) : IQuery<IReadOnlyList<MapObjectReadDto>>;

public sealed class MapObjectReadDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string TypeCode { get; init; } = string.Empty;
    public LocationReadDto Location { get; init; } = new();
    public float RotationYaw { get; init; }
    public Guid WorldId { get; init; }
    public string ZoneId { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ComponentReadDto> Components { get; init; } = new List<ComponentReadDto>();
    public IReadOnlyDictionary<string, string> State { get; init; } = new Dictionary<string, string>();
    public DateTime LastUpdated { get; init; }
    public ContainerComponentDto? Container { get; set; }
    public LockableComponentDto? Lockable { get; set; }
    public DoorComponentDto? Door { get; set; }
}

public sealed class ContainerComponentDto
{
    public IReadOnlyList<InventorySlotDto> Items { get; init; } = new List<InventorySlotDto>();
}

public sealed class LockableComponentDto
{
    public bool IsLocked { get; init; }
    public string? RequiredKeyItemId { get; init; }
    public int LockpickDifficulty { get; init; }
    public bool CanBeLockpicked { get; init; }
}

public sealed class DoorComponentDto
{
    public bool IsOpen { get; init; }
    public Guid? LinkedDoorId { get; init; }
    public string? OpenAnimation { get; init; }
    public string? CloseAnimation { get; init; }
    public float OpenAngle { get; init; }
    public bool AutoClose { get; init; }
    public int AutoCloseDelaySeconds { get; init; }
}

public sealed class GetMapObjectQueryHandler(IModelRepository repo) : IQueryHandler<GetMapObjectQuery, MapObjectReadDto>
{
    public async Task<MapObjectReadDto> HandleAsync(GetMapObjectQuery query, CancellationToken ct = default)
    {
        var mo = await repo.GetByIdAsync<MapObject>(query.MapObjectId, ct) ?? throw new KeyNotFoundException("MapObject not found");
        return MapObjectQueriesMapper.Map(mo);
    }
}

public sealed class GetMapObjectsQueryHandler(IModelRepository repo) : IQueryHandler<GetMapObjectsQuery, IReadOnlyList<MapObjectReadDto>>
{
    public async Task<IReadOnlyList<MapObjectReadDto>> HandleAsync(GetMapObjectsQuery query, CancellationToken ct = default)
    {
        var all = await repo.GetAllAsync<MapObject>(ct);
        return all.Select(MapObjectQueriesMapper.Map).ToList();
    }
}

public sealed class GetMapObjectsByIdsQueryHandler(IModelRepository repo) : IQueryHandler<GetMapObjectsByIdsQuery, IReadOnlyList<MapObjectReadDto>>
{
    public async Task<IReadOnlyList<MapObjectReadDto>> HandleAsync(GetMapObjectsByIdsQuery query, CancellationToken ct = default)
    {
        var list = new List<MapObjectReadDto>(query.MapObjectIds.Count);
        foreach (var id in query.MapObjectIds)
        {
            var mo = await repo.GetByIdAsync<MapObject>(id, ct);
            if (mo != null) list.Add(MapObjectQueriesMapper.Map(mo));
        }
        return list;
    }
}

internal static class MapObjectQueriesMapper
{
    public static MapObjectReadDto Map(MapObject mo)
    {
        var dto = new MapObjectReadDto
        {
            Id = mo.Id,
            Name = mo.Name,
            DisplayName = mo.DisplayName,
            Description = mo.Description,
            TypeCode = string.Empty,
            Location = LocationReadDto.FromDomain(mo.Location),
            RotationYaw = mo.RotationYaw,
            WorldId = mo.WorldId,
            ZoneId = mo.ZoneId,
            IsActive = mo.IsActive,
            Tags = mo.Tags.ToList(),
            Components = mo.Components.Select(c => new ComponentReadDto(c.GetType().Name, JsonSerializer.Serialize(c, c.GetType()))).ToList(),
            State = mo.State?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? new Dictionary<string, string>(),
            LastUpdated = mo.LastUpdated
        };

        if (mo.GetComponent<ContainerComponent>() is { } cont)
        {
            dto.Container = new ContainerComponentDto
            {
                Items = cont.Items.Select(s => new InventorySlotDto
                {
                    ItemId = s.Item?.Id,
                    Quantity = s.Quantity
                }).ToList()
            };
        }
        if (mo.GetComponent<LockableComponent>() is { } lockable)
        {
            dto.Lockable = new LockableComponentDto
            {
                IsLocked = lockable.IsLocked,
                RequiredKeyItemId = lockable.RequiredKeyItemId,
                LockpickDifficulty = lockable.LockpickDifficulty,
                CanBeLockpicked = lockable.CanBeLockpicked
            };
        }
        if (mo.GetComponent<DoorComponent>() is { } door)
        {
            dto.Door = new DoorComponentDto
            {
                IsOpen = door.IsOpen,
                LinkedDoorId = door.LinkedDoorId,
                OpenAnimation = door.OpenAnimation,
                CloseAnimation = door.CloseAnimation,
                OpenAngle = door.OpenAngle,
                AutoClose = door.AutoClose,
                AutoCloseDelaySeconds = door.AutoCloseDelaySeconds
            };
        }

        return dto;
    }
}
