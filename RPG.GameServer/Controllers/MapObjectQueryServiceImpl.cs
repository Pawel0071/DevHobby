using Grpc.Core;
using RPG.GameServer.QueryProtos;
using RPG.Application.Interfaces;
using RPG.Application.Queries;

namespace RPG.GameServer.Controllers;

public class MapObjectQueryServiceImpl(IQueryBus queryBus) : MapObjectQuery.MapObjectQueryBase
{
    public override async Task<MapObjectSingleReply> GetMapObject(MapObjectGetByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id)) throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Id"));
        try
        {
            var dto = await queryBus.ExecuteAsync<GetMapObjectQuery, MapObjectReadDto>(new GetMapObjectQuery(id), context.CancellationToken);
            return new MapObjectSingleReply { Mo = Map(dto) };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "MapObject not found"));
        }
    }

    public override async Task<MapObjectListReply> ListMapObjects(MapObjectListRequest request, ServerCallContext context)
    {
        var list = await queryBus.ExecuteAsync<GetMapObjectsQuery, IReadOnlyList<MapObjectReadDto>>(new GetMapObjectsQuery(), context.CancellationToken);
        var reply = new MapObjectListReply();
        reply.Mos.AddRange(list.Select(Map));
        return reply;
    }

    public override async Task<MapObjectListReply> GetMapObjectsByIds(MapObjectGetByIdsRequest request, ServerCallContext context)
    {
        var ids = request.Ids.Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null).Where(g => g.HasValue).Select(g => g!.Value).ToArray();
        var list = await queryBus.ExecuteAsync<GetMapObjectsByIdsQuery, IReadOnlyList<MapObjectReadDto>>(new GetMapObjectsByIdsQuery(ids), context.CancellationToken);
        var reply = new MapObjectListReply();
        reply.Mos.AddRange(list.Select(Map));
        return reply;
    }

    private static MapObject Map(MapObjectReadDto dto)
    {
        var msg = new MapObject
        {
            Id = dto.Id.ToString(),
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            TypeCode = dto.TypeCode,
            Location = new Location
            {
                X = dto.Location.X,
                Y = dto.Location.Y,
                Z = dto.Location.Z,
                WorldId = dto.Location.WorldId ?? string.Empty,
                MapId = dto.Location.MapId,
                ZoneName = dto.Location.ZoneName,
                Rotation = dto.Location.Rotation
            },
            RotationYaw = dto.RotationYaw,
            WorldId = dto.WorldId.ToString(),
            ZoneId = dto.ZoneId,
            IsActive = dto.IsActive,
            LastUpdatedUnixMs = new DateTimeOffset(dto.LastUpdated).ToUnixTimeMilliseconds()
        };
        msg.Tags.AddRange(dto.Tags);
        foreach (var kv in dto.State) msg.State[kv.Key] = kv.Value;
        foreach (var c in dto.Components)
        {
            msg.Components.Add(new Component { Type = c.Type, Data = c.Data });
        }
        if (dto.Container != null)
        {
            var c = new ContainerComponentTyped();
            foreach (var it in dto.Container.Items)
            {
                c.Items.Add(new InventorySlot { ItemId = it.ItemId?.ToString() ?? string.Empty, Quantity = it.Quantity });
            }
            msg.Container = c;
        }
        if (dto.Lockable != null)
        {
            msg.Lockable = new LockableComponentTyped
            {
                IsLocked = dto.Lockable.IsLocked,
                RequiredKeyItemId = dto.Lockable.RequiredKeyItemId ?? string.Empty,
                LockpickDifficulty = dto.Lockable.LockpickDifficulty,
                CanBeLockpicked = dto.Lockable.CanBeLockpicked
            };
        }
        if (dto.Door != null)
        {
            msg.Door = new DoorComponentTyped
            {
                IsOpen = dto.Door.IsOpen,
                LinkedDoorId = dto.Door.LinkedDoorId?.ToString() ?? string.Empty,
                OpenAnimation = dto.Door.OpenAnimation ?? string.Empty,
                CloseAnimation = dto.Door.CloseAnimation ?? string.Empty,
                OpenAngle = dto.Door.OpenAngle,
                AutoClose = dto.Door.AutoClose,
                AutoCloseDelaySeconds = dto.Door.AutoCloseDelaySeconds
            };
        }
        return msg;
    }
}
