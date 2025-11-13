// filepath: /Volumes/Data/Repositories/DevHobby/RPG.GameServer/Mappers/DtoMappers.cs
using RPG.GameServer.Dtos;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Skills;

namespace RPG.GameServer.Mappers;

public static class DtoMappers
{
    public static ItemDto ToDto(this Item item)
    {
        // Compute optional component-derived fields first
        IReadOnlyDictionary<string, int>? modifiers = null;
        int? socketNo = null;
        IReadOnlyCollection<Guid>? skillIds = null;
        Guid? questId = null;
        Guid? stepId = null;
        IReadOnlyCollection<RPG.Domain.Enums.EquipmentSlot>? equipmentSlots = null;
        bool? isTwoHanded = null;
        bool? supportsDualWield = null;
        bool? isUniqueEquip = null;
        IReadOnlyCollection<string>? usedInItemIds = null;

        if (item.GetComponent<RPG.Domain.Models.Items.ItemComponent.StatsComponent>() is { } stats && stats.Stats is { } statContainer)
        {
            modifiers = statContainer.Stats.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value
            );
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

        var dto = new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            TypeCode = item.TypeCode,
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

        return dto;
    }

    public static NpcDto ToDto(this Npc npc)
        => new()
        {
            Id = npc.Id,
            Name = npc.Name,
            Level = npc.Level,
            IsMoving = npc.IsMoving,
            X = npc.CurrentLocation.Position.X,
            Y = npc.CurrentLocation.Position.Y,
            Z = npc.CurrentLocation.Position.Z,
            Rotation = npc.CurrentLocation.Rotation,
            Tags = npc.Tags.ToList()
        };

    public static SkillDto ToDto(this Skill skill)
        => new()
        {
            Id = skill.Id,
            Name = skill.Name,
            Tags = skill.Tags.ToList()
        };
}
