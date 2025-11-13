using Grpc.Core;
using RPG.GameServer.QueryProtos;
using RPG.Application.Interfaces;
using RPG.Application.Queries;

namespace RPG.GameServer.Controllers;

public class NpcQueryServiceImpl(IQueryBus queryBus) : NpcQuery.NpcQueryBase
{
    public override async Task<NpcSingleReply> GetNpc(NpcGetByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id)) throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Id"));
        try
        {
            var dto = await queryBus.ExecuteAsync<GetNpcQuery, NpcReadDto>(new GetNpcQuery(id), context.CancellationToken);
            return new NpcSingleReply { Npc = Map(dto) };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Npc not found"));
        }
    }

    public override async Task<NpcListReply> ListNpcs(NpcListRequest request, ServerCallContext context)
    {
        var list = await queryBus.ExecuteAsync<GetNpcsQuery, IReadOnlyList<NpcReadDto>>(new GetNpcsQuery(), context.CancellationToken);
        var reply = new NpcListReply();
        reply.Npcs.AddRange(list.Select(Map));
        return reply;
    }

    public override async Task<NpcListReply> GetNpcsByIds(NpcGetByIdsRequest request, ServerCallContext context)
    {
        var ids = request.Ids
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToArray();
        var list = await queryBus.ExecuteAsync<GetNpcsByIdsQuery, IReadOnlyList<NpcReadDto>>(new GetNpcsByIdsQuery(ids), context.CancellationToken);
        var reply = new NpcListReply();
        reply.Npcs.AddRange(list.Select(Map));
        return reply;
    }

    private static Npc Map(NpcReadDto dto)
    {
        var msg = new Npc
        {
            Id = dto.Id.ToString(),
            Name = dto.Name,
            Level = dto.Level,
            IsMoving = dto.IsMoving,
            X = dto.CurrentLocation.X,
            Y = dto.CurrentLocation.Y,
            Z = dto.CurrentLocation.Z,
            Rotation = dto.CurrentLocation.Rotation
        };
        msg.Tags.AddRange(dto.Tags);
        return msg;
    }
}
