using Grpc.Core;
using RPG.GameServer.QueryProtos;
using RPG.GameServer.Mappers;
using RPG.Application.Interfaces;
using RPG.Application.Queries;
using DomainItem = RPG.Domain.Models.Items.Item;

namespace RPG.GameServer.Controllers;

public class ItemQueryServiceImpl : ItemQuery.ItemQueryBase
{
    private readonly IQueryBus _queryBus;
    private readonly ItemProtoMapper _mapper;

    public ItemQueryServiceImpl(IQueryBus queryBus, ItemProtoMapper mapper)
    {
        _queryBus = queryBus;
        _mapper = mapper;
    }

    public override async Task<ItemSingleReply> GetItem(ItemGetByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Id"));

        try
        {
            var item = await _queryBus.ExecuteAsync<GetItemQuery, DomainItem>(new GetItemQuery(id), context.CancellationToken);
            return new ItemSingleReply { Item = _mapper.ToProto(item) };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Item not found"));
        }
    }

    public override async Task<ItemListReply> ListItems(ItemListRequest request, ServerCallContext context)
    {
        var items = await _queryBus.ExecuteAsync<GetItemsQuery, IReadOnlyList<DomainItem>>(new GetItemsQuery(), context.CancellationToken);
        var reply = new ItemListReply();
        reply.Items.AddRange(items.Select(_mapper.ToProto));
        return reply;
    }

    public override async Task<ItemListReply> GetItemsByIds(ItemGetByIdsRequest request, ServerCallContext context)
    {
        var ids = request.Ids
            .Select(s => Guid.TryParse(s, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToArray();

        var items = await _queryBus.ExecuteAsync<GetItemsByIdsQuery, IReadOnlyList<DomainItem>>(new GetItemsByIdsQuery(ids), context.CancellationToken);
        var reply = new ItemListReply();
        reply.Items.AddRange(items.Select(_mapper.ToProto));
        return reply;
    }
}
