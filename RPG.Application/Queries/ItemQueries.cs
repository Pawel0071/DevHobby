// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Queries/ItemQueries.cs
using RPG.Application.Interfaces;
using RPG.Domain.Models.Items;
using RPG.Infrastructure.Interfaces;
using RPG.Domain.Enums;

namespace RPG.Application.Queries;

public sealed record GetItemQuery(Guid ItemId) : IQuery<Item>;
public sealed record GetItemsQuery() : IQuery<IReadOnlyList<Item>>;
public sealed record GetItemsByIdsQuery(IReadOnlyCollection<Guid> ItemIds) : IQuery<IReadOnlyList<Item>>;

public sealed class GetItemQueryHandler(IModelRepository repo) : IQueryHandler<GetItemQuery, Item>
{
    public async Task<Item> HandleAsync(GetItemQuery query, CancellationToken ct = default)
        => await repo.GetByIdAsync<Item>(query.ItemId, ct) ?? throw new KeyNotFoundException("Item not found");
}

public sealed class GetItemsQueryHandler(IModelRepository repo) : IQueryHandler<GetItemsQuery, IReadOnlyList<Item>>
{
    public async Task<IReadOnlyList<Item>> HandleAsync(GetItemsQuery query, CancellationToken ct = default)
        => (await repo.GetAllAsync<Item>(ct)).ToList();
}

public sealed class GetItemsByIdsQueryHandler(IModelRepository repo) : IQueryHandler<GetItemsByIdsQuery, IReadOnlyList<Item>>
{
    public async Task<IReadOnlyList<Item>> HandleAsync(GetItemsByIdsQuery query, CancellationToken ct = default)
    {
        var list = new List<Item>(query.ItemIds.Count);
        foreach (var id in query.ItemIds)
        {
            var item = await repo.GetByIdAsync<Item>(id, ct);
            if (item != null) list.Add(item);
        }
        return list;
    }
}
