using Grpc.Core;
using RPG.GameServer.QueryProtos;
using RPG.Application.Interfaces;
using RPG.Application.Queries;

namespace RPG.GameServer.Controllers;

public class ItemQueryServiceImpl(IQueryBus queryBus) : ItemQuery.ItemQueryBase
{
    public override async Task<ItemSingleReply> GetItem(ItemGetByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id)) throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Id"));
        try
        {
            var dto = await queryBus.ExecuteAsync<GetItemQuery, ItemReadDto>(new GetItemQuery(id), context.CancellationToken);
            return new ItemSingleReply { Item = Map(dto) };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Item not found"));
        }
    }

    public override async Task<ItemListReply> ListItems(ItemListRequest request, ServerCallContext context)
    {
        var list = await queryBus.ExecuteAsync<GetItemsQuery, IReadOnlyList<ItemReadDto>>(new GetItemsQuery(), context.CancellationToken);
        var reply = new ItemListReply();
        reply.Items.AddRange(list.Select(Map));
        return reply;
    }

    public override async Task<ItemListReply> GetItemsByIds(ItemGetByIdsRequest request, ServerCallContext context)
    {
        var ids = request.Ids
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToArray();
        var list = await queryBus.ExecuteAsync<GetItemsByIdsQuery, IReadOnlyList<ItemReadDto>>(new GetItemsByIdsQuery(ids), context.CancellationToken);
        var reply = new ItemListReply();
        reply.Items.AddRange(list.Select(Map));
        return reply;
    }

    private static Item Map(ItemReadDto dto)
    {
        var msg = new Item
        {
            Id = dto.Id.ToString(),
            Name = dto.Name,
            TypeCode = dto.TypeCode,
            RequiredLevel = dto.RequiredLevel,
            StackSize = dto.StackSize
        };
        msg.Tags.AddRange(dto.Tags);
        if (dto.Modifiers != null)
        {
            foreach (var kv in dto.Modifiers)
                msg.Modifiers[kv.Key] = kv.Value;
        }
        if (dto.SocketNo.HasValue) msg.SocketNo = dto.SocketNo.Value;
        if (dto.SkillIds != null) msg.SkillIds.AddRange(dto.SkillIds.Select(g => g.ToString()));
        if (dto.QuestId.HasValue) msg.QuestId = dto.QuestId.Value.ToString();
        if (dto.StepId.HasValue) msg.StepId = dto.StepId.Value.ToString();
        if (dto.EquipmentSlots != null) msg.EquipmentSlots.AddRange(dto.EquipmentSlots.Select(s => s.ToString()));
        if (dto.IsTwoHanded.HasValue) msg.IsTwoHanded = dto.IsTwoHanded.Value;
        if (dto.SupportsDualWield.HasValue) msg.SupportsDualWield = dto.SupportsDualWield.Value;
        if (dto.IsUniqueEquip.HasValue) msg.IsUniqueEquip = dto.IsUniqueEquip.Value;
        if (dto.UsedInItemIds != null) msg.UsedInItemIds.AddRange(dto.UsedInItemIds);
        return msg;
    }
}
