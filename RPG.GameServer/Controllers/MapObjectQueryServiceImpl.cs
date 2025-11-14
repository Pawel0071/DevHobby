using Grpc.Core;
using RPG.GameServer.QueryProtos;
using RPG.GameServer.Mappers;
using RPG.Application.Interfaces;
using RPG.Application.Queries;
using DomainMapObject = RPG.Domain.Models.MapObjects.MapObject;

namespace RPG.GameServer.Controllers;

public class MapObjectQueryServiceImpl : MapObjectQuery.MapObjectQueryBase
{
    private readonly IQueryBus _queryBus;
    private readonly MapObjectProtoMapper _mapper;

    public MapObjectQueryServiceImpl(IQueryBus queryBus, MapObjectProtoMapper mapper)
    {
        _queryBus = queryBus;
        _mapper = mapper;
    }

    public override async Task<MapObjectSingleReply> GetMapObject(MapObjectGetByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id)) throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Id"));
        try
        {
            var mo = await _queryBus.ExecuteAsync<GetMapObjectQuery, DomainMapObject>(new GetMapObjectQuery(id), context.CancellationToken);
            return new MapObjectSingleReply { Mo = _mapper.ToProto(mo) };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "MapObject not found"));
        }
    }

    public override async Task<MapObjectListReply> ListMapObjects(MapObjectListRequest request, ServerCallContext context)
    {
        var list = await _queryBus.ExecuteAsync<GetMapObjectsQuery, IReadOnlyList<DomainMapObject>>(new GetMapObjectsQuery(), context.CancellationToken);
        var reply = new MapObjectListReply();
        reply.Mos.AddRange(list.Select(_mapper.ToProto));
        return reply;
    }

    public override async Task<MapObjectListReply> GetMapObjectsByIds(MapObjectGetByIdsRequest request, ServerCallContext context)
    {
        var ids = request.Ids.Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null).Where(g => g.HasValue).Select(g => g!.Value).ToArray();
        var list = await _queryBus.ExecuteAsync<GetMapObjectsByIdsQuery, IReadOnlyList<DomainMapObject>>(new GetMapObjectsByIdsQuery(ids), context.CancellationToken);
        var reply = new MapObjectListReply();
        reply.Mos.AddRange(list.Select(_mapper.ToProto));
        return reply;
    }
}

