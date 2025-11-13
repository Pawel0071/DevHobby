using System.Numerics;
using RPG.Application.Interfaces;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Skills;

namespace RPG.Application.Commands;

public record EquipItemCommand(Guid CharacterId, EquipmentSlot Slot, Item Item) : ICommand;

public record UnequipItemCommand(Guid CharacterId, EquipmentSlot Slot) : ICommand;

public record PutItemToBankCommand(Guid CharacterId, Item Item) : ICommand;

public record GetItemFromBankCommand(Guid CharacterId, Item Item) : ICommand;

public record UseItemCommand(Guid CharacterId, Item Item) : ICommand;

public record DropItemCommand(Guid CharacterId, Item Item) : ICommand;

public record PickUpItemCommand(Guid CharacterId, Item Item) : ICommand;

public record GainExperienceCommand(Guid CharacterId, int Amount) : ICommand;

public record LevelUpCommand(Guid CharacterId) : ICommand;

public record StartMovementCommand(Guid CharacterId, int Direction, bool PreserveFacing = false) : ICommand;

public record StopMovementCommand(Guid CharacterId) : ICommand;

public record StartRotationCommand(Guid CharacterId, int Direction) : ICommand;

public record StopRotationCommand(Guid CharacterId) : ICommand;

public record JumpCommand(Guid CharacterId) : ICommand;

public record DashCommand(Guid CharacterId, Vector3 TargetPosition) : ICommand;

public record TeleportCommand(Guid CharacterId, Vector3 TargetPosition) : ICommand;

public record StartBasicAttackCommand(Guid CharacterId, Guid TargetId) : ICommand;

public record StopBasicAttackCommand(Guid CharacterId) : ICommand;

public record CastSkillCommand(Guid CharacterId, Guid SkillId, Vector3 TargetPosition) : ICommand;

public record InterruptCastCommand(Guid CharacterId) : ICommand;

public record LearnSkillCommand(Guid CharacterId, Guid SkillId) : ICommand;

public record RespecSkillsCommand(Guid CharacterId) : ICommand;

public record ApplyBuffCommand(Guid CharacterId, Skill Buff) : ICommand;

public record RemoveBuffCommand(Guid CharacterId, Skill Buff): ICommand;

public record ApplyDebuffCommand(Guid CharacterId, Skill Debuff): ICommand;

public record DieCommand(Guid CharacterId): ICommand;

public record LoginCharacterCommand(Guid CharacterId): ICommand;

public record LogoutCharacterCommand(Guid CharacterId): ICommand;

public record SaveCharacterStateCommand(Guid CharacterId): ICommand;

public record LoadCharacterStateCommand(Guid CharacterId): ICommand;

public record CreateCharacterCommand(
    Guid CharacterId,
    Guid SessionId,
    string Name,
    CharacterClass CharacterClass,
    int Level,
    int MaxHealth,
    int MaxResource,
    float? X = null,
    float? Y = null,
    float? Z = null,
    Guid? WorldId = null,
    string? MapId = null,
    string? ZoneName = null,
    float? Rotation = null,
    bool IsMoving = false,
    bool IsRotating = false,
    IStatsContainer? Stats = null) : ICommand;
