using Grpc.Core;
using RPG.GameServer.QueryProtos;
using RPG.GameServer.Mappers;
using RPG.Application.Interfaces;
using RPG.Application.Queries;
using DomainQuest = RPG.Domain.Models.Quests.Quest;

namespace RPG.GameServer.Controllers;

public class QuestQueryServiceImpl : QuestQuery.QuestQueryBase
{
    private readonly IQueryBus _queryBus;
    private readonly QuestProtoMapper _mapper;

    public QuestQueryServiceImpl(IQueryBus queryBus, QuestProtoMapper mapper)
    {
        _queryBus = queryBus;
        _mapper = mapper;
    }

    public override async Task<QuestSingleReply> GetQuest(QuestGetByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id)) throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Id"));
        try
        {
            var quest = await _queryBus.ExecuteAsync<GetQuestQuery, DomainQuest>(new GetQuestQuery(id), context.CancellationToken);
            return new QuestSingleReply { Quest = _mapper.ToProto(quest) };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Quest not found"));
        }
    }

    public override async Task<QuestListReply> ListQuests(QuestListRequest request, ServerCallContext context)
    {
        var list = await _queryBus.ExecuteAsync<GetQuestsQuery, IReadOnlyList<DomainQuest>>(new GetQuestsQuery(), context.CancellationToken);
        var reply = new QuestListReply();
        reply.Quests.AddRange(list.Select(_mapper.ToProto));
        return reply;
    }

    public override async Task<QuestListReply> GetQuestsByIds(QuestGetByIdsRequest request, ServerCallContext context)
    {
        var ids = request.Ids.Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null).Where(g => g.HasValue).Select(g => g!.Value).ToArray();
        var list = await _queryBus.ExecuteAsync<GetQuestsByIdsQuery, IReadOnlyList<DomainQuest>>(new GetQuestsByIdsQuery(ids), context.CancellationToken);
        var reply = new QuestListReply();
        reply.Quests.AddRange(list.Select(_mapper.ToProto));
        return reply;
    }
}

