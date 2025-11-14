// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Queries/MapObjectQueries.cs
using RPG.Application.Interfaces;
using RPG.Domain.Models.MapObjects;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Queries;

public sealed record GetMapObjectQuery(Guid MapObjectId) : IQuery<MapObject>;
public sealed record GetMapObjectsQuery() : IQuery<IReadOnlyList<MapObject>>;
public sealed record GetMapObjectsByIdsQuery(IReadOnlyCollection<Guid> MapObjectIds) : IQuery<IReadOnlyList<MapObject>>;

public sealed class GetMapObjectQueryHandler(IModelRepository repo) : IQueryHandler<GetMapObjectQuery, MapObject>
{
    public async Task<MapObject> HandleAsync(GetMapObjectQuery query, CancellationToken ct = default)
    {
        return await repo.GetByIdAsync<MapObject>(query.MapObjectId, ct) ?? throw new KeyNotFoundException("MapObject not found");
    }
}

public sealed class GetMapObjectsQueryHandler(IModelRepository repo) : IQueryHandler<GetMapObjectsQuery, IReadOnlyList<MapObject>>
{
    public async Task<IReadOnlyList<MapObject>> HandleAsync(GetMapObjectsQuery query, CancellationToken ct = default)
    {
        var all = await repo.GetAllAsync<MapObject>(ct);
        return all.ToList();
    }
}

public sealed class GetMapObjectsByIdsQueryHandler(IModelRepository repo) : IQueryHandler<GetMapObjectsByIdsQuery, IReadOnlyList<MapObject>>
{
    public async Task<IReadOnlyList<MapObject>> HandleAsync(GetMapObjectsByIdsQuery query, CancellationToken ct = default)
    {
        var list = new List<MapObject>(query.MapObjectIds.Count);
        foreach (var id in query.MapObjectIds)
        {
            var mo = await repo.GetByIdAsync<MapObject>(id, ct);
            if (mo != null) list.Add(mo);
        }
        return list;
    }
}
