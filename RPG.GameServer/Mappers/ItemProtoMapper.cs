using RPG.GameServer.QueryProtos;
using DomainItem = RPG.Domain.Models.Items.Item;
using RPG.Domain.Models.Items.ItemComponent;
using RPG.Domain.Enums;

namespace RPG.GameServer.Mappers;

/// <summary>
/// Mapper for Item domain model to proto message
/// </summary>
public class ItemProtoMapper : IProtoMapper<DomainItem, Item>
{
    private readonly RPG.Infrastructure.Interfaces.ILogger<ItemProtoMapper> _logger;

    public ItemProtoMapper(RPG.Infrastructure.Interfaces.ILogger<ItemProtoMapper> logger)
    {
        _logger = logger;
    }

    public Item ToProto(DomainItem domain)
    {
        _logger.Debug($"Converting Item to proto. Id={domain.Id}, Name={domain.Name}");

        var proto = new Item
        {
            Id = domain.Id.ToString(),
            Name = domain.Name,
            TypeCode = domain.TypeCode,
            RequiredLevel = domain.RequiredLevel,
            StackSize = domain.StackSize
        };

        // Tags
        proto.Tags.AddRange(domain.Tags);

        // Component-derived fields
        // Stats -> Modifiers
        if (domain.GetComponent<StatsComponent>() is { } stats && stats.Stats is { } statContainer)
        {
            foreach (var kvp in statContainer.Stats)
            {
                proto.Modifiers[kvp.Key.ToString()] = kvp.Value;
            }
        }

        // Socket -> SocketNo
        if (domain.GetComponent<SocketComponent>() is { } socket)
        {
            proto.SocketNo = socket.SocketNo;
        }

        // SkillGrant -> SkillIds
        if (domain.GetComponent<SkillGrantComponent>() is { } skills && skills.SkillIds is { Count: > 0 })
        {
            proto.SkillIds.AddRange(skills.SkillIds.Select(id => id.ToString()));
        }

        // QuestItem -> QuestId, StepId
        if (domain.GetComponent<QuestItemComponent>() is { } quest)
        {
            if (quest.QuestId != Guid.Empty)
                proto.QuestId = quest.QuestId.ToString();
            if (quest.StepId != Guid.Empty)
                proto.StepId = quest.StepId.ToString();
        }

        // Equippable -> EquipmentSlots, IsTwoHanded, SupportsDualWield, IsUniqueEquip
        if (domain.GetComponent<EquippableComponent>() is { } equippable)
        {
            if (equippable.ValidSlots is { Count: > 0 })
                proto.EquipmentSlots.AddRange(equippable.ValidSlots.Select(s => s.ToString()));

            proto.IsTwoHanded = equippable.IsTwoHanded;
            proto.SupportsDualWield = equippable.SupportsDualWield;
            proto.IsUniqueEquip = equippable.IsUniqueEquip;
        }

        // CraftMaterial -> UsedInItemIds
        if (domain.GetComponent<CraftMaterialComponent>() is { } material && material.UsedInItemIds is { Count: > 0 })
        {
            proto.UsedInItemIds.AddRange(material.UsedInItemIds);
        }

        _logger.Debug($"Item proto created. Id={proto.Id}, Tags={proto.Tags.Count}");
        return proto;
    }

    public DomainItem ToDomain(Item proto)
    {
        _logger.Debug($"Converting Item proto to domain. Id={proto.Id}, Name={proto.Name}");

        var id = Guid.TryParse(proto.Id, out var parsed) ? parsed : Guid.NewGuid();
        var item = new DomainItem(id, proto.TypeCode)
        {
            Name = proto.Name,
            RequiredLevel = proto.RequiredLevel,
            StackSize = proto.StackSize
        };

        // Tags
        foreach (var tag in proto.Tags)
        {
            item.Tags.Add(tag);
        }

        // Modifiers -> Stats component
        if (proto.Modifiers != null && proto.Modifiers.Count > 0)
        {
            var stats = new RPG.Domain.Containers.StatsContainer();
            foreach (var kvp in proto.Modifiers)
            {
                if (Enum.TryParse<StatsProperty>(kvp.Key, true, out var prop))
                {
                    stats.Stats[prop] = kvp.Value;
                }
            }
            if (stats.Stats.Count > 0)
            {
                item.Components.Add(new StatsComponent { Stats = stats });
            }
        }

        // Socket
        if (proto.SocketNo > 0)
        {
            item.Components.Add(new SocketComponent { SocketNo = proto.SocketNo });
        }

        // SkillGrant
        if (proto.SkillIds is { Count: > 0 })
        {
            var skillIds = proto.SkillIds
                .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();
            if (skillIds.Count > 0)
            {
                item.Components.Add(new SkillGrantComponent { SkillIds = skillIds });
            }
        }

        // QuestItem
        if (!string.IsNullOrEmpty(proto.QuestId) || !string.IsNullOrEmpty(proto.StepId))
        {
            var questId = Guid.TryParse(proto.QuestId, out var qId) ? qId : Guid.Empty;
            var stepId = Guid.TryParse(proto.StepId, out var sId) ? sId : Guid.Empty;
            item.Components.Add(new QuestItemComponent { QuestId = questId, StepId = stepId });
        }

        // Equippable
        if (proto.EquipmentSlots is { Count: > 0 } || proto.IsTwoHanded || proto.SupportsDualWield || proto.IsUniqueEquip)
        {
            var slots = proto.EquipmentSlots
                .Select(s => Enum.TryParse<EquipmentSlot>(s, out var slot) ? slot : (EquipmentSlot?)null)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();

            item.Components.Add(new EquippableComponent
            {
                ValidSlots = slots,
                IsTwoHanded = proto.IsTwoHanded,
                SupportsDualWield = proto.SupportsDualWield,
                IsUniqueEquip = proto.IsUniqueEquip
            });
        }

        // CraftMaterial
        if (proto.UsedInItemIds is { Count: > 0 })
        {
            item.Components.Add(new CraftMaterialComponent { UsedInItemIds = proto.UsedInItemIds.ToList() });
        }

        _logger.Debug($"Item domain created. Id={item.Id}, Components={item.Components.Count}");
        return item;
    }
}

