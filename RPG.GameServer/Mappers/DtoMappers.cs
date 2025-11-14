// filepath: /Volumes/Data/Repositories/DevHobby/RPG.GameServer/Mappers/DtoMappers.cs
using RPG.GameServer.Dtos;
using RPG.GameServer.Protos;
using RPG.Domain.Enums;
using RPG.Domain.Models.Items.ItemComponent;
using RPG.Domain.Containers;
// Usuń niejednoznaczności przez aliasy
using DomainItem = RPG.Domain.Models.Items.Item;
using DomainSkill = RPG.Domain.Models.Skills.Skill;
using DomainCharacter = RPG.Domain.Models.Character;
using DomainLocation = RPG.Domain.Models.Location;
using DomainNpc = RPG.Domain.Models.Npcs.Npc;

namespace RPG.GameServer.Mappers;

public static class DtoMappers
{
    public static ItemDto ToDto(this DomainItem item)
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

    public static NpcDto ToDto(this DomainNpc npc)
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

    public static SkillDto ToDto(this DomainSkill skill)
        => new()
        {
            Id = skill.Id,
            Name = skill.Name,
            Tags = skill.Tags.ToList()
        };

    public static DomainCharacter ToDomainCharacter(this CharacterRequest request)
    {
        if (request?.Character?.BaseCharacter is null)
            throw new ArgumentNullException(nameof(request), "CharacterRequest.Character.BaseCharacter is required");

        var pc = request.Character;
        var bc = pc.BaseCharacter;

        var sessionId = Guid.TryParse(pc.SessionId, out var s) ? s : Guid.NewGuid();
        var characterId = Guid.TryParse(bc.Id, out var c) ? c : Guid.NewGuid();
        var cls = MapCharacterClass(pc.CharacterClass);

        var character = new DomainCharacter(sessionId, cls)
        {
            Id = characterId,
            Name = bc.Name,
            Level = bc.Level > 0 ? bc.Level : 1,
            MaxHealth = bc.MaxHealth > 0 ? bc.MaxHealth : 100,
            CurrentHealth = bc.CurrentHealth > 0 ? bc.CurrentHealth : (bc.MaxHealth > 0 ? bc.MaxHealth : 100),
            MaxResource = bc.MaxMana > 0 ? bc.MaxMana : 60,
            CurrentResource = bc.CurrentMana > 0 ? bc.CurrentMana : 0
        };

        // Stats → BaseStats (+ zapewnij minimalny Modified MoveSpeed)
        if (bc.Stats is not null)
        {
            character.BaseStats[StatsProperty.Strength] = bc.Stats.Strength;
            character.BaseStats[StatsProperty.Agility] = bc.Stats.Agility;
            character.BaseStats[StatsProperty.Intelligence] = bc.Stats.Intelligence;
            character.BaseStats[StatsProperty.Wisdom] = bc.Stats.Wisdom;
            character.BaseStats[StatsProperty.Dexterity] = bc.Stats.Dexterity;
            character.BaseStats[StatsProperty.Vitality] = bc.Stats.Vitality;
            character.BaseStats[StatsProperty.MagicResist] = bc.Stats.MagicResist;
            character.BaseStats[StatsProperty.NatureResist] = bc.Stats.NatureResist;
            character.BaseStats[StatsProperty.MisticResist] = bc.Stats.MisticResist;
            character.BaseStats[StatsProperty.Armor] = bc.Stats.Armor;
            character.BaseStats[StatsProperty.CritChance] = bc.Stats.CritChance;
            character.BaseStats[StatsProperty.HitChance] = bc.Stats.HitChance;
            character.BaseStats[StatsProperty.AttackSpeed] = bc.Stats.AttackSpeed;
            character.BaseStats[StatsProperty.MoveSpeed] = bc.Stats.MoveSpeed;

            character.ModifiedStats[StatsProperty.MoveSpeed] = bc.Stats.MoveSpeed;
        }

        // Location
        var x = (float)(bc.Position?.X ?? 0);
        var y = (float)(bc.Position?.Y ?? 0);
        var z = (float)(bc.Position?.Z ?? 0);
        var worldId = Guid.TryParse(bc.Position?.WorldId, out var parsedWorldId) ? parsedWorldId : Guid.Empty;
        var location = DomainLocation.Create(x, y, z, worldId, bc.Position?.MapId ?? string.Empty, bc.Position?.ZoneName ?? string.Empty);
        location.Rotation = bc.Position != null ? bc.Position.Rotation : bc.Rotation;
        character.SetCurrentLocation(location);

        // Movement flags (neutral, tylko stan inicjalny)
        character.SetMovementState(bc.IsMoving);
        character.SetRotationState(bc.IsRotating);

        // Equipment → Domain (mapuj tylko dostępne pola)
        if (pc.Equipment is not null)
        {
            var eq = character.GetEquipmentContainer();
            if (pc.Equipment.Head is not null)
                eq[RPG.Domain.Enums.EquipmentSlot.Head] = pc.Equipment.Head.ToDomainItem();
            if (pc.Equipment.Chest is not null)
                eq[RPG.Domain.Enums.EquipmentSlot.Chest] = pc.Equipment.Chest.ToDomainItem();
            if (pc.Equipment.Weapon is not null)
                eq[RPG.Domain.Enums.EquipmentSlot.Weapon1] = pc.Equipment.Weapon.ToDomainItem();
            if (pc.Equipment.Shield is not null)
                eq[RPG.Domain.Enums.EquipmentSlot.Weapon2] = pc.Equipment.Shield.ToDomainItem();
            if (pc.Equipment.Boots is not null)
                eq[RPG.Domain.Enums.EquipmentSlot.Feet] = pc.Equipment.Boots.ToDomainItem();
            if (pc.Equipment.Gloves is not null)
                eq[RPG.Domain.Enums.EquipmentSlot.Hands] = pc.Equipment.Gloves.ToDomainItem();
            if (pc.Equipment.Amulet is not null)
                eq[RPG.Domain.Enums.EquipmentSlot.Amulet] = pc.Equipment.Amulet.ToDomainItem();
            if (pc.Equipment.Rings is { Count: > 0 })
            {
                var ringIndex = 0;
                foreach (var ring in pc.Equipment.Rings)
                {
                    if (ring == null) continue;
                    var slot = ringIndex == 0 ? RPG.Domain.Enums.EquipmentSlot.Ring1 : RPG.Domain.Enums.EquipmentSlot.Ring2;
                    eq[slot] = ring.ToDomainItem();
                    ringIndex++;
                    if (ringIndex > 1) break;
                }
            }
        }

        // Inventory → Domain (wrzuć do pierwszych slotów plecaka)
        if (pc.Inventory is { Count: > 0 })
        {
            var inv = character.GetBackpackInventoryContainer();
            var index = 0;
            foreach (var protoItem in pc.Inventory)
            {
                if (protoItem == null) continue;
                if (index >= inv.Capacity) break;
                inv[index] = protoItem.ToDomainItem();
                index++;
            }
        }

        // Skills: różnica typów Id (proto:int vs domain:Guid) – zgodnie z architekturą
        // inicjalne przypisanie umiejętności powinno wykonać Core (np. przez eventy lub seeding).

        return character;
    }

    private static DomainItem ToDomainItem(this Protos.Item p)
    {
        var id = Guid.TryParse(p.Id, out var parsed) ? parsed : Guid.NewGuid();
        var typeCode = p.Type.ToString();
        var item = new DomainItem(id, typeCode)
        {
            Name = p.Name,
            RequiredLevel = p.RequiredLevel,
            StackSize = 1
        };

        if (p.Modifiers != null && p.Modifiers.Count > 0)
        {
            var stats = new StatsContainer();
            foreach (var kvp in p.Modifiers)
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

        return item;
    }

    private static RPG.Domain.Enums.CharacterClass MapCharacterClass(Protos.CharacterClass protoClass)
    {
        var name = protoClass.ToString();
        return Enum.TryParse<RPG.Domain.Enums.CharacterClass>(name, true, out var parsed)
            ? parsed
            : RPG.Domain.Enums.CharacterClass.Warrior;
    }
}
