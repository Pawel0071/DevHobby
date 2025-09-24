using Grpc.Core;
using RabbitMQ.Client;
using RPG.GameServer.Interfaces;
using StackExchange.Redis;
using DomainPlayerCharacter = RPG.Core.Domain.Entities.PlayerCharacter;
using RPG.GameServer.Protos;
using ProtobufBaseCharacter = RPG.GameServer.Protos.BaseCharacter;
using ProtobufPlayerCharacter = RPG.GameServer.Protos.PlayerCharacter;
using ProtobufStats = RPG.GameServer.Protos.Stats;
using ProtobufLocation = RPG.GameServer.Protos.Location;
using ProtobufSkill = RPG.GameServer.Protos.ProtobufSkill;
using ProtobufSkillType = RPG.GameServer.Protos.SkillType;
using ProtobufEquipmentSlots = RPG.GameServer.Protos.EquipmentSlots;
using ProtobufItem = RPG.GameServer.Protos.ProtobufItem;


using DomainStats = RPG.Core.Domain.Entities.Common.Stats;
using DomainLocation = RPG.Core.Domain.Entities.Common.Location;
using DomainSkill = RPG.Core.Domain.Entities.Skill;
using DomainEquipmentSlots = RPG.Core.Domain.Entities.Common.EquipmentSlots;
using DomainEffect = RPG.Core.Domain.Entities.Common.Effect;
using DomainItem = RPG.Core.Domain.Entities.Common.Item;
using DomainItemType = RPG.Core.Domain.Entities.Common.ItemType;

namespace RPG.GameServer.Services;

public class CharacterServiceImpl : CharacterService.CharacterServiceBase
{
    private readonly IDatabase _redis;
    private readonly IModel _rabbitChannel;
    private readonly ICharacterRepository _characterRepository;

    public CharacterServiceImpl (IConnectionMultiplexer redis, IModel rabbitChannel, ICharacterRepository characterRepository)
    {
        _redis = redis.GetDatabase();
        _rabbitChannel = rabbitChannel;
        _characterRepository = characterRepository;
    }

    // Update mapping logic to align with regenerated Protobuf classes
    private DomainPlayerCharacter MapToDomainPlayerCharacter(ProtobufPlayerCharacter character)
    {
        return new DomainPlayerCharacter
        {
            Id = character.BaseCharacter.Id,
            Name = character.BaseCharacter.Name,
            Level = character.BaseCharacter.Level,
            MaxHealth = character.BaseCharacter.MaxHealth,
            CurrentHealth = character.BaseCharacter.CurrentHealth,
            MaxMana = character.BaseCharacter.MaxMana,
            CurrentMana = character.BaseCharacter.CurrentMana,
            Stats = new DomainStats
            {
                Strength = character.BaseCharacter.Stats.Strength,
                Dexterity = character.BaseCharacter.Stats.Dexterity,
                Intelligence = character.BaseCharacter.Stats.Intelligence,
                Vitality = character.BaseCharacter.Stats.Vitality,
                Wisdom = character.BaseCharacter.Stats.Wisdom
            },
            Position = new DomainLocation
            {
                X = (float)character.BaseCharacter.Position.X,
                Y = (float)character.BaseCharacter.Position.Y,
                Z = (float)character.BaseCharacter.Position.Z
            },
            Skills = character.BaseCharacter.Skills.Select(skill => new DomainSkill
            {
                Id = skill.Id,
                Name = skill.Name,
                Type = (DomainSkillType)(int)skill.Type,
                ManaCost = skill.ManaCost,
                CooldownSeconds = skill.CooldownSeconds
            }).ToList(),
            SkillCooldowns = character.BaseCharacter.SkillCooldowns.ToDictionary(
                kvp => kvp.Key,
                kvp => DateTime.Parse(kvp.Value)
            ),
            ActiveEffects = character.BaseCharacter.ActiveEffects.Select(effect => (DomainEffect)(int)effect).ToList(),
            Equipment = new DomainEquipmentSlots
            {
                Head = MapToDomainItem(character.Equipment.Head),
                Chest = MapToDomainItem(character.Equipment.Chest),
                Weapon = MapToDomainItem(character.Equipment.Weapon),
                Shield = MapToDomainItem(character.Equipment.Shield),
                Boots = MapToDomainItem(character.Equipment.Boots),
                Gloves = MapToDomainItem(character.Equipment.Gloves),
                Rings = character.Equipment.Rings?.Select(MapToDomainItem).ToList(),
                Amulet = MapToDomainItem(character.Equipment.Amulet)
            },
            Inventory = character.Inventory.Select(MapToDomainItem).ToList(),
            SkillLevels = character.SkillLevels.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
            ),
            SessionId = Guid.TryParse(character.SessionId, out var sessionId) ? sessionId : (Guid?)null
        };
    }

    // Update MapToProtobufPlayerCharacter to include all properties
    private ProtobufPlayerCharacter MapToProtobufPlayerCharacter(DomainPlayerCharacter character)
    {
        return new ProtobufPlayerCharacter         
        {
            BaseCharacter = new ProtobufBaseCharacter
            {
                Id = character.Id,
                Name = character.Name,
                Level = character.Level,
                MaxHealth = character.MaxHealth,
                CurrentHealth = character.CurrentHealth,
                MaxMana = character.MaxMana,
                CurrentMana = character.CurrentMana,
                Stats = new ProtobufStats
                {
                    Strength = character.Stats.Strength,
                    Dexterity = character.Stats.Dexterity,
                    Intelligence = character.Stats.Intelligence,
                    Vitality = character.Stats.Vitality,
                    Wisdom = character.Stats.Wisdom
                },
                Position = new ProtobufLocation
                {
                    X = character.Position.X,
                    Y = character.Position.Y,
                    Z = character.Position.Z
                },
                Skills = { character.Skills.Select(skill => new ProtobufSkill
                {
                    Id = skill.Id,
                    Name = skill.Name,
                    Type = (SkillType)(int)skill.Type,
                    ManaCost = skill.ManaCost,
                    CooldownSeconds = skill.CooldownSeconds
                }) },
                SkillCooldowns = { character.SkillCooldowns.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToString("o") // Convert DateTime to ISO 8601 string
                ) },
                ActiveEffects = { character.ActiveEffects.Select(effect => (Effect)(int)effect) }
            },
            Equipment = new ProtobufEquipmentSlots
            {
                Head = MapToProtobufItem(character.Equipment.Head),
                Chest = MapToProtobufItem(character.Equipment.Chest),
                Weapon = MapToProtobufItem(character.Equipment.Weapon),
                Shield = MapToProtobufItem(character.Equipment.Shield),
                Boots = MapToProtobufItem(character.Equipment.Boots),
                Gloves = MapToProtobufItem(character.Equipment.Gloves),
                Rings = { character.Equipment.Rings.Select(MapToProtobufItem) },
                Amulet = MapToProtobufItem(character.Equipment.Amulet)
            },
            Inventory = { character.Inventory.Select(MapToProtobufItem) },
            SkillLevels = { character.SkillLevels },
            SessionId = character.SessionId.ToString()
        };
    }

    // Handle nullability in MapToDomainItem
    private DomainItem MapToDomainItem(ProtobufItem item)
    {
        if (item == null)
        {
            return new DomainItem
            {
                Id = string.Empty,
                Name = string.Empty,
                Type = DomainItemType.Miscellaneous,
                Modifiers = new Dictionary<string, int>(),
                RequiredLevel = 0
            };
        }

        return new DomainItem
        {
            Id = item.Id,
            Name = item.Name,
            Type = (DomainItemType)item.Type,
            Modifiers = item.Modifiers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            RequiredLevel = item.RequiredLevel
        };
    }

    private ProtobufItem MapToProtobufItem(DomainItem domainItem)
    {
        if (domainItem == null)
        {
            return new ProtobufItem
            {
                Id = string.Empty,
                Name = string.Empty,
                Type = ItemType.Miscellaneous, // Corrected enum value
                Modifiers = { },
                RequiredLevel = 0
            };
        }

        return new ProtobufItem 
        {
            Id = domainItem.Id,
            Name = domainItem.Name,
            Type = (ItemType)(int)domainItem.Type,
            Modifiers = { (domainItem.Modifiers ?? new Dictionary<string, int>()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) },
            RequiredLevel = domainItem.RequiredLevel
        };
    }

    // Adjust return types to match method signatures
    public override async Task<CharacterIdReply> CreateCharacter(CharacterRequest request, ServerCallContext context)
    {
        if (request.Character == null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Character is required"));
        }
        var domainCharacter = MapToDomainPlayerCharacter(request.Character);
        var character = await _characterRepository.CreateAsync(domainCharacter);
        return new CharacterIdReply { CharacterId = character.Id };
    }

    public override async Task<CharacterReply> GetCharacter(CharacterIdRequest request, ServerCallContext context)
    {
        var character = await _characterRepository.GetAsync(request.CharacterId);
        if (character == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Character not found"));
        }
        return new CharacterReply { Character = MapToProtobufPlayerCharacter(character) };
    }

    public override async Task<CharacterIdReply> UpdateCharacter(CharacterRequest request, ServerCallContext context)
    {
        if (request.Character == null || string.IsNullOrEmpty(request.Character.BaseCharacter?.Id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Character or Character ID is required"));
        }
        var domainCharacter = MapToDomainPlayerCharacter(request.Character);
        var character = await _characterRepository.UpdateAsync(domainCharacter);
        return new CharacterIdReply {   CharacterId = character.Id };
    }

    public override async Task<CharacterIdReply> DeleteCharacter(CharacterIdRequest request, ServerCallContext context)
    {
        var success = await _characterRepository.DeleteAsync(request.CharacterId);
        return new CharacterIdReply { CharacterId = success ? character.Id : 0 };
    }
}