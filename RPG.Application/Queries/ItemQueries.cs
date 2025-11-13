// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Queries/ItemQueries.cs
using RPG.Application.Interfaces;
using RPG.Domain.Models.Items;
using RPG.Infrastructure.Interfaces;
using RPG.Domain.Enums;

namespace RPG.Application.Queries;

public sealed record GetItemQuery(Guid ItemId) : IQuery<ItemReadDto>;
public sealed record GetItemsQuery() : IQuery<IReadOnlyList<ItemReadDto>>;
public sealed record GetItemsByIdsQuery(IReadOnlyCollection<Guid> ItemIds) : IQuery<IReadOnlyList<ItemReadDto>>;

public sealed class ItemReadDto
{
    public Guid Id { get; init; }
    public string TypeCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int RequiredLevel { get; init; }
    public int StackSize { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
    // komponenty spłaszczone analogicznie do ItemDocument
    public IReadOnlyDictionary<string, int>? Modifiers { get; init; }
    public int? SocketNo { get; init; }
    public IReadOnlyCollection<Guid>? SkillIds { get; init; }
    public Guid? QuestId { get; init; }
    public Guid? StepId { get; init; }
    public IReadOnlyCollection<EquipmentSlot>? EquipmentSlots { get; init; }
    public bool? IsTwoHanded { get; init; }
    public bool? SupportsDualWield { get; init; }
    public bool? IsUniqueEquip { get; init; }
    public IReadOnlyCollection<string>? UsedInItemIds { get; init; }
}

public sealed class GetItemQueryHandler(IModelRepository repo) : IQueryHandler<GetItemQuery, ItemReadDto>
{
    public async Task<ItemReadDto> HandleAsync(GetItemQuery query, CancellationToken ct = default)
    {
        var item = await repo.GetByIdAsync<Item>(query.ItemId, ct) ?? throw new KeyNotFoundException("Item not found");
        return ItemQueriesMapper.Map(item);
    }
}

public sealed class GetItemsQueryHandler(IModelRepository repo) : IQueryHandler<GetItemsQuery, IReadOnlyList<ItemReadDto>>
{
    public async Task<IReadOnlyList<ItemReadDto>> HandleAsync(GetItemsQuery query, CancellationToken ct = default)
    {
        var all = await repo.GetAllAsync<Item>(ct);
        return all.Select(ItemQueriesMapper.Map).ToList();
    }
}

public sealed class GetItemsByIdsQueryHandler(IModelRepository repo) : IQueryHandler<GetItemsByIdsQuery, IReadOnlyList<ItemReadDto>>
{
    public async Task<IReadOnlyList<ItemReadDto>> HandleAsync(GetItemsByIdsQuery query, CancellationToken ct = default)
    {
        var list = new List<ItemReadDto>(query.ItemIds.Count);
        foreach (var id in query.ItemIds)
        {
            var item = await repo.GetByIdAsync<Item>(id, ct);
            if (item != null) list.Add(ItemQueriesMapper.Map(item));
        }
        return list;
    }
}

internal static class ItemQueriesMapper
{
    public static ItemReadDto Map(Item item)
    {
        IReadOnlyDictionary<string, int>? modifiers = null;
        int? socketNo = null;
        IReadOnlyCollection<Guid>? skillIds = null;
        Guid? questId = null;
        Guid? stepId = null;
        IReadOnlyCollection<EquipmentSlot>? equipmentSlots = null;
        bool? isTwoHanded = null;
        bool? supportsDualWield = null;
        bool? isUniqueEquip = null;
        IReadOnlyCollection<string>? usedInItemIds = null;

        if (item.GetComponent<RPG.Domain.Models.Items.ItemComponent.StatsComponent>() is { } stats && stats.Stats is { } statContainer)
        {
            modifiers = statContainer.Stats.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
        }
        if (item.GetComponent<RPG.Domain.Models.Items.ItemComponent.SocketComponent>() is { } socket)
            socketNo = socket.SocketNo;
        if (item.GetComponent<RPG.Domain.Models.Items.ItemComponent.SkillGrantComponent>() is { } skills)
            skillIds = skills.SkillIds?.ToList();
        if (item.GetComponent<RPG.Domain.Models.Items.ItemComponent.QuestItemComponent>() is { } quest)
        {
            questId = quest.QuestId;
            stepId = quest.StepId;
        }
        if (item.GetComponent<RPG.Domain.Models.Items.ItemComponent.EquippableComponent>() is { } equippable)
        {
            equipmentSlots = equippable.ValidSlots?.ToList();
            isTwoHanded = equippable.IsTwoHanded;
            supportsDualWield = equippable.SupportsDualWield;
            isUniqueEquip = equippable.IsUniqueEquip;
        }
        if (item.GetComponent<RPG.Domain.Models.Items.ItemComponent.CraftMaterialComponent>() is { } material)
        {
            usedInItemIds = material.UsedInItemIds?.ToList();
        }

        return new ItemReadDto
        {
            Id = item.Id,
            TypeCode = item.TypeCode,
            Name = item.Name,
            RequiredLevel = item.RequiredLevel,
            StackSize = item.StackSize,
            Tags = item.Tags.ToList(),
            Modifiers = modifiers,
            SocketNo = socketNo,
            SkillIds = skillIds,
            QuestId = questId,
            StepId = stepId,
            EquipmentSlots = equipmentSlots,
            IsTwoHanded = isTwoHanded,
            SupportsDualWield = supportsDualWield,
            IsUniqueEquip = isUniqueEquip,
            UsedInItemIds = usedInItemIds
        };
    }
}
