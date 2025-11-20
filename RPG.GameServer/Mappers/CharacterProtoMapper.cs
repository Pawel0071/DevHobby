using RPG.GameServer.Protos;
using RPG.Domain.Enums;
using RPG.Domain.Containers;
using CharacterClass = RPG.Domain.Enums.CharacterClass;
using DomainCharacter = RPG.Domain.Models.Character;

namespace RPG.GameServer.Mappers;

/// <summary>
/// Mapper for Character domain model to proto message (for Character creation requests)
/// </summary>
public class CharacterProtoMapper
{
    private readonly Infrastructure.Interfaces.ILogger<CharacterProtoMapper> _logger;
    private readonly LocationProtoMapper _locationMapper;

    public CharacterProtoMapper(Infrastructure.Interfaces.ILogger<CharacterProtoMapper> logger,
        LocationProtoMapper locationMapper)
    {
        _logger = logger;
        _locationMapper = locationMapper;
    }

    /// <summary>
    /// Converts CharacterRequest proto to domain Character
    /// </summary>
    public DomainCharacter ToDomain(CharacterRequest request)
    {
        if (request?.Character?.BaseCharacter is null)
            throw new ArgumentNullException(nameof(request), "CharacterRequest.Character.BaseCharacter is required");

        _logger.Debug($"Converting CharacterRequest proto to Character domain. Name={request.Character.BaseCharacter.Name}");

        var pc = request.Character;
        var bc = pc.BaseCharacter;

        var sessionId = Guid.TryParse(pc.SessionId, out var s) ? s : Guid.NewGuid();
        var characterId = Guid.TryParse(bc.Id, out var c) ? c : Guid.NewGuid();
        var cls = MapCharacterClass(pc.CharacterClass);

        var character = new DomainCharacter(sessionId, cls)
        {
            Id = characterId,
            Name = bc.Name,
            Level = bc.Level > 0
                ? bc.Level
                : 1,
            MaxHealth = bc.MaxHealth > 0
                ? bc.MaxHealth
                : 100,
            CurrentHealth = bc.CurrentHealth > 0
                ? bc.CurrentHealth
                : (bc.MaxHealth > 0
                    ? bc.MaxHealth
                    : 100),
            MaxResource = bc.MaxMana > 0
                ? bc.MaxMana
                : 60,
            CurrentResource = bc.CurrentMana > 0
                ? bc.CurrentMana
                : 0,
            Class = cls
        };

        // Stats → BaseStats
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
            character.BaseStats[StatsProperty.FireResist] = bc.Stats.FireResist;
            character.BaseStats[StatsProperty.FrostResist] = bc.Stats.FrostResist;
            character.BaseStats[StatsProperty.Armor] = bc.Stats.Armor;
            character.BaseStats[StatsProperty.CritChance] = bc.Stats.CritChance;
            character.BaseStats[StatsProperty.CritDamage] = bc.Stats.CritDamage;
            character.BaseStats[StatsProperty.HitChance] = bc.Stats.HitChance;
            character.BaseStats[StatsProperty.AttackSpeed] = bc.Stats.AttackSpeed;
            character.BaseStats[StatsProperty.MoveSpeed] = bc.Stats.MoveSpeed;
            character.BaseStats[StatsProperty.MeleeAttackPower] = bc.Stats.MeleeAttackPower;
            character.BaseStats[StatsProperty.RangedAttackPower] = bc.Stats.RangedAttackPower;
            character.BaseStats[StatsProperty.MagicAttackPower] = bc.Stats.MagicAttackPower;
            character.BaseStats[StatsProperty.NatureAttackPower] = bc.Stats.NatureAttackPower;
            character.BaseStats[StatsProperty.FireAttackPower] = bc.Stats.FireAttackPower;
            character.BaseStats[StatsProperty.FrostAttackPower] = bc.Stats.FrostAttackPower;

            character.ModifiedStats[StatsProperty.MoveSpeed] = bc.Stats.MoveSpeed;
        }

        // Location
        var x = (float)(bc.Position?.X ?? 0);
        var y = (float)(bc.Position?.Y ?? 0);
        var z = (float)(bc.Position?.Z ?? 0);
        var worldId = Guid.TryParse(bc.Position?.WorldId, out var parsedWorldId) ? parsedWorldId : Guid.Empty;
        var location = RPG.Domain.Models.Location.Create(x, y, z, worldId, bc.Position?.MapId ?? string.Empty, bc.Position?.ZoneName ?? string.Empty);
        location.Direction = bc.Position != null ? bc.Position.Rotation : bc.Rotation;
        character.CurrentLocation =location;

        // Movement flags
        character.IsMoving = bc.IsMoving;
        character.IsRotating = bc.IsRotating ;

        // Equipment → Domain
        if (pc.Equipment is not null)
        {
            var eq = character.Equipments;
            if (pc.Equipment.Head is not null)
                eq[EquipmentSlot.Head] = ToItem(pc.Equipment.Head);
            if (pc.Equipment.Chest is not null)
                eq[EquipmentSlot.Chest] = ToItem(pc.Equipment.Chest);
            if (pc.Equipment.Weapon is not null)
                eq[EquipmentSlot.Weapon1] = ToItem(pc.Equipment.Weapon);
            if (pc.Equipment.Shield is not null)
                eq[EquipmentSlot.Weapon2] = ToItem(pc.Equipment.Shield);
            if (pc.Equipment.Boots is not null)
                eq[EquipmentSlot.Feet] = ToItem(pc.Equipment.Boots);
            if (pc.Equipment.Gloves is not null)
                eq[EquipmentSlot.Hands] = ToItem(pc.Equipment.Gloves);
            if (pc.Equipment.Amulet is not null)
                eq[EquipmentSlot.Amulet] = ToItem(pc.Equipment.Amulet);
            if (pc.Equipment.Rings is { Count: > 0 })
            {
                var ringIndex = 0;
                foreach (var ring in pc.Equipment.Rings)
                {
                    if (ring == null) continue;
                    var slot = ringIndex == 0 ? EquipmentSlot.Ring1 : EquipmentSlot.Ring2;
                    eq[slot] = ToItem(ring);
                    ringIndex++;
                    if (ringIndex > 1) break;
                }
            }
        }

        // Inventory → Domain
        if (pc.Inventory is { Count: > 0 })
        {
            var inv = character.BackpackInventory;
            var capacity = inv.Count;
            var index = 0;
            foreach (var protoItem in pc.Inventory)
            {
                if (protoItem == null) continue;
                if (index >= capacity) break;
                inv[index].Item = ToItem(protoItem);
                inv[index].Quantity = 1;
                index++;
            }
        }

        _logger.Debug($"Character domain created. Id={character.Id}, Name={character.Name}");
        return character;
    }

    private static RPG.Domain.Models.Items.Item ToItem(Protos.Item p)
    {
        var id = Guid.TryParse(p.Id, out var parsed) ? parsed : Guid.NewGuid();
        var typeCode = p.Type.ToString();
        var item = new RPG.Domain.Models.Items.Item(id, typeCode)
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
                item.Components.Add(new RPG.Domain.Models.Items.ItemComponent.StatsComponent { Stats = stats });
            }
        }

        return item;
    }

    private static CharacterClass MapCharacterClass(Protos.CharacterClass protoClass)
    {
        var name = protoClass.ToString();
        return Enum.TryParse<CharacterClass>(name, true, out var parsed)
            ? parsed
            : CharacterClass.Warrior;
    }
}
