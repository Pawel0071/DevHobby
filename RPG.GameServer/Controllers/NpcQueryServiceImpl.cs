using Grpc.Core;
using RPG.GameServer.QueryProtos;
using RPG.GameServer.Mappers;
using RPG.Application.Interfaces;
using RPG.Application.Queries;
using DomainNpc = RPG.Domain.Models.Npcs.Npc;

namespace RPG.GameServer.Controllers;

public class NpcQueryServiceImpl : NpcQuery.NpcQueryBase
{
    private readonly IQueryBus _queryBus;
    private readonly NpcProtoMapper _mapper;

    public NpcQueryServiceImpl(IQueryBus queryBus, NpcProtoMapper mapper)
    {
        _queryBus = queryBus;
        _mapper = mapper;
    }

    public override async Task<NpcSingleReply> GetNpc(NpcGetByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id)) throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Id"));
        try
        {
            var npc = await _queryBus.ExecuteAsync<GetNpcQuery, DomainNpc>(new GetNpcQuery(id), context.CancellationToken);
            return new NpcSingleReply { Npc = _mapper.ToProto(npc) };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Npc not found"));
        }
    }

    public override async Task<NpcListReply> ListNpcs(NpcListRequest request, ServerCallContext context)
    {
        var list = await _queryBus.ExecuteAsync<GetNpcsQuery, IReadOnlyList<DomainNpc>>(new GetNpcsQuery(), context.CancellationToken);
        var reply = new NpcListReply();
        reply.Npcs.AddRange(list.Select(_mapper.ToProto));
        return reply;
    }

    public override async Task<NpcListReply> GetNpcsByIds(NpcGetByIdsRequest request, ServerCallContext context)
    {
        var ids = request.Ids
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToArray();
        var list = await _queryBus.ExecuteAsync<GetNpcsByIdsQuery, IReadOnlyList<DomainNpc>>(new GetNpcsByIdsQuery(ids), context.CancellationToken);
        var reply = new NpcListReply();
        reply.Npcs.AddRange(list.Select(_mapper.ToProto));
        return reply;
    }
}
