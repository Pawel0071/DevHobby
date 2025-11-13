using Grpc.Core;
using RPG.GameServer.QueryProtos;
using RPG.Application.Interfaces;
using RPG.Application.Queries;

namespace RPG.GameServer.Controllers;

public class SkillQueryServiceImpl(IQueryBus queryBus) : SkillQuery.SkillQueryBase
{
    public override async Task<SkillSingleReply> GetSkill(SkillGetByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id)) throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Id"));
        try
        {
            var dto = await queryBus.ExecuteAsync<GetSkillQuery, SkillReadDto>(new GetSkillQuery(id), context.CancellationToken);
            return new SkillSingleReply { Skill = Map(dto) };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Skill not found"));
        }
    }

    public override async Task<SkillListReply> ListSkills(SkillListRequest request, ServerCallContext context)
    {
        var list = await queryBus.ExecuteAsync<GetSkillsQuery, IReadOnlyList<SkillReadDto>>(new GetSkillsQuery(), context.CancellationToken);
        var reply = new SkillListReply();
        reply.Skills.AddRange(list.Select(Map));
        return reply;
    }

    public override async Task<SkillListReply> GetSkillsByIds(SkillGetByIdsRequest request, ServerCallContext context)
    {
        var ids = request.Ids
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToArray();
        var list = await queryBus.ExecuteAsync<GetSkillsByIdsQuery, IReadOnlyList<SkillReadDto>>(new GetSkillsByIdsQuery(ids), context.CancellationToken);
        var reply = new SkillListReply();
        reply.Skills.AddRange(list.Select(Map));
        return reply;
    }

    private static Skill Map(SkillReadDto dto)
    {
        var msg = new Skill { Id = dto.Id.ToString(), Name = dto.Name };
        msg.Tags.AddRange(dto.Tags);
        return msg;
    }
}
