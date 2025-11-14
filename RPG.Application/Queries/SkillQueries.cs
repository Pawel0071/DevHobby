// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Queries/SkillQueries.cs
using RPG.Application.Interfaces;
using RPG.Domain.Models.Skills;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Queries;

public sealed record GetSkillQuery(Guid SkillId) : IQuery<Skill>;
public sealed record GetSkillsQuery() : IQuery<IReadOnlyList<Skill>>;
public sealed record GetSkillsByIdsQuery(IReadOnlyCollection<Guid> SkillIds) : IQuery<IReadOnlyList<Skill>>;

public sealed class GetSkillQueryHandler(IModelRepository repo) : IQueryHandler<GetSkillQuery, Skill>
{
    public async Task<Skill> HandleAsync(GetSkillQuery query, CancellationToken ct = default)
    {
        return await repo.GetByIdAsync<Skill>(query.SkillId, ct) ?? throw new KeyNotFoundException("Skill not found");
    }
}

public sealed class GetSkillsQueryHandler(IModelRepository repo) : IQueryHandler<GetSkillsQuery, IReadOnlyList<Skill>>
{
    public async Task<IReadOnlyList<Skill>> HandleAsync(GetSkillsQuery query, CancellationToken ct = default)
    {
        var all = await repo.GetAllAsync<Skill>(ct);
        return all.ToList();
    }
}

public sealed class GetSkillsByIdsQueryHandler(IModelRepository repo) : IQueryHandler<GetSkillsByIdsQuery, IReadOnlyList<Skill>>
{
    public async Task<IReadOnlyList<Skill>> HandleAsync(GetSkillsByIdsQuery query, CancellationToken ct = default)
    {
        var list = new List<Skill>(query.SkillIds.Count);
        foreach (var id in query.SkillIds)
        {
            var skill = await repo.GetByIdAsync<Skill>(id, ct);
            if (skill != null) list.Add(skill);
        }
        return list;
    }
}
