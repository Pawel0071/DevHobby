// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Queries/NpcQueries.cs
using RPG.Application.Interfaces;
using RPG.Domain.Models.Npcs;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Queries;

public sealed record GetNpcQuery(Guid NpcId) : IQuery<Npc>;
public sealed record GetNpcsQuery() : IQuery<IReadOnlyList<Npc>>;
public sealed record GetNpcsByIdsQuery(IReadOnlyCollection<Guid> NpcIds) : IQuery<IReadOnlyList<Npc>>;

public sealed class GetNpcQueryHandler(IModelRepository repo) : IQueryHandler<GetNpcQuery, Npc>
{
    public async Task<Npc> HandleAsync(GetNpcQuery query, CancellationToken ct = default)
    {
        return await repo.GetByIdAsync<Npc>(query.NpcId, ct) ?? throw new KeyNotFoundException("Npc not found");
    }
}

public sealed class GetNpcsQueryHandler(IModelRepository repo) : IQueryHandler<GetNpcsQuery, IReadOnlyList<Npc>>
{
    public async Task<IReadOnlyList<Npc>> HandleAsync(GetNpcsQuery query, CancellationToken ct = default)
    {
        var all = await repo.GetAllAsync<Npc>(ct);
        return all.ToList();
    }
}

public sealed class GetNpcsByIdsQueryHandler(IModelRepository repo) : IQueryHandler<GetNpcsByIdsQuery, IReadOnlyList<Npc>>
{
    public async Task<IReadOnlyList<Npc>> HandleAsync(GetNpcsByIdsQuery query, CancellationToken ct = default)
    {
        var list = new List<Npc>(query.NpcIds.Count);
        foreach (var id in query.NpcIds)
        {
            var npc = await repo.GetByIdAsync<Npc>(id, ct);
            if (npc != null) list.Add(npc);
        }
        return list;
    }
}
