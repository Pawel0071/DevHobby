// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Queries/SkillQueries.cs
using RPG.Application.Interfaces;
using RPG.Domain.Models.Skills;
using RPG.Infrastructure.Interfaces;
using System.Text.Json;

namespace RPG.Application.Queries;

public sealed record GetSkillQuery(Guid SkillId) : IQuery<SkillReadDto>;
public sealed record GetSkillsQuery() : IQuery<IReadOnlyList<SkillReadDto>>;
public sealed record GetSkillsByIdsQuery(IReadOnlyCollection<Guid> SkillIds) : IQuery<IReadOnlyList<SkillReadDto>>;

public sealed class SkillReadDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconId { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ComponentReadDto> Components { get; init; } = new List<ComponentReadDto>();
}

public sealed class GetSkillQueryHandler(IModelRepository repo) : IQueryHandler<GetSkillQuery, SkillReadDto>
{
    public async Task<SkillReadDto> HandleAsync(GetSkillQuery query, CancellationToken ct = default)
    {
        var skill = await repo.GetByIdAsync<Skill>(query.SkillId, ct) ?? throw new KeyNotFoundException("Skill not found");
        return SkillQueriesMapper.Map(skill);
    }
}

public sealed class GetSkillsQueryHandler(IModelRepository repo) : IQueryHandler<GetSkillsQuery, IReadOnlyList<SkillReadDto>>
{
    public async Task<IReadOnlyList<SkillReadDto>> HandleAsync(GetSkillsQuery query, CancellationToken ct = default)
    {
        var all = await repo.GetAllAsync<Skill>(ct);
        return all.Select(SkillQueriesMapper.Map).ToList();
    }
}

public sealed class GetSkillsByIdsQueryHandler(IModelRepository repo) : IQueryHandler<GetSkillsByIdsQuery, IReadOnlyList<SkillReadDto>>
{
    public async Task<IReadOnlyList<SkillReadDto>> HandleAsync(GetSkillsByIdsQuery query, CancellationToken ct = default)
    {
        var list = new List<SkillReadDto>(query.SkillIds.Count);
        foreach (var id in query.SkillIds)
        {
            var skill = await repo.GetByIdAsync<Skill>(id, ct);
            if (skill != null) list.Add(SkillQueriesMapper.Map(skill));
        }
        return list;
    }
}

internal static class SkillQueriesMapper
{
    public static SkillReadDto Map(Skill skill) => new()
    {
        Id = skill.Id,
        Name = skill.Name,
        Description = skill.Description,
        IconId = skill.IconId,
        Tags = skill.Tags.ToList(),
        Components = skill.Components.Select(c => new ComponentReadDto(c.GetType().Name, JsonSerializer.Serialize(c, c.GetType()))).ToList()
    };
}
