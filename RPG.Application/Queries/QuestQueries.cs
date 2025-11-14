// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Queries/QuestQueries.cs
using RPG.Application.Interfaces;
using RPG.Domain.Models.Quests;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Queries;

public sealed record GetQuestQuery(Guid QuestId) : IQuery<Quest>;
public sealed record GetQuestsQuery() : IQuery<IReadOnlyList<Quest>>;
public sealed record GetQuestsByIdsQuery(IReadOnlyCollection<Guid> QuestIds) : IQuery<IReadOnlyList<Quest>>;

public sealed class GetQuestQueryHandler(IModelRepository repo) : IQueryHandler<GetQuestQuery, Quest>
{
    public async Task<Quest> HandleAsync(GetQuestQuery query, CancellationToken ct = default)
    {
        return await repo.GetByIdAsync<Quest>(query.QuestId, ct) ?? throw new KeyNotFoundException("Quest not found");
    }
}

public sealed class GetQuestsQueryHandler(IModelRepository repo) : IQueryHandler<GetQuestsQuery, IReadOnlyList<Quest>>
{
    public async Task<IReadOnlyList<Quest>> HandleAsync(GetQuestsQuery query, CancellationToken ct = default)
    {
        var all = await repo.GetAllAsync<Quest>(ct);
        return all.ToList();
    }
}

public sealed class GetQuestsByIdsQueryHandler(IModelRepository repo) : IQueryHandler<GetQuestsByIdsQuery, IReadOnlyList<Quest>>
{
    public async Task<IReadOnlyList<Quest>> HandleAsync(GetQuestsByIdsQuery query, CancellationToken ct = default)
    {
        var list = new List<Quest>(query.QuestIds.Count);
        foreach (var id in query.QuestIds)
        {
            var quest = await repo.GetByIdAsync<Quest>(id, ct);
            if (quest != null) list.Add(quest);
        }
        return list;
    }
}
