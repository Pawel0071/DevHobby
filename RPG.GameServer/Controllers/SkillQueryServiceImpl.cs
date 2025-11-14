using Grpc.Core;
using RPG.GameServer.QueryProtos;
using RPG.GameServer.Mappers;
using RPG.Application.Interfaces;
using RPG.Application.Queries;
using DomainSkill = RPG.Domain.Models.Skills.Skill;

namespace RPG.GameServer.Controllers;

public class SkillQueryServiceImpl : SkillQuery.SkillQueryBase
{
    private readonly IQueryBus _queryBus;
    private readonly SkillProtoMapper _mapper;

    public SkillQueryServiceImpl(IQueryBus queryBus, SkillProtoMapper mapper)
    {
        _queryBus = queryBus;
        _mapper = mapper;
    }

    public override async Task<SkillSingleReply> GetSkill(SkillGetByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Id"));
        try
        {
            var skill = await _queryBus.ExecuteAsync<GetSkillQuery, DomainSkill>(new GetSkillQuery(id), context.CancellationToken);
            return new SkillSingleReply { Skill = _mapper.ToProto(skill) };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Skill not found"));
        }
    }

    public override async Task<SkillListReply> ListSkills(SkillListRequest request, ServerCallContext context)
    {
        var list = await _queryBus.ExecuteAsync<GetSkillsQuery, IReadOnlyList<DomainSkill>>(new GetSkillsQuery(), context.CancellationToken);
        var reply = new SkillListReply();
        reply.Skills.AddRange(list.Select(_mapper.ToProto));
        return reply;
    }

    public override async Task<SkillListReply> GetSkillsByIds(SkillGetByIdsRequest request, ServerCallContext context)
    {
        var ids = request.Ids
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToArray();
        var list = await _queryBus.ExecuteAsync<GetSkillsByIdsQuery, IReadOnlyList<DomainSkill>>(new GetSkillsByIdsQuery(ids), context.CancellationToken);
        var reply = new SkillListReply();
        reply.Skills.AddRange(list.Select(_mapper.ToProto));
        return reply;
    }
}
