using System.Numerics;
using RPG.Application.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Domain.Enums;

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

public record StartMovementCommand(Guid CharacterId, int Direction) : ICommand;
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


public record ApplyBuffCommand(Guid CharacterId, Skill Buff);
public record RemoveBuffCommand(Guid CharacterId, Skill Buff);
public record ApplyDebuffCommand(Guid CharacterId, Skill Debuff);
public record DieCommand(Guid CharacterId);

public record LoginCharacterCommand(Guid CharacterId);
public record LogoutCharacterCommand(Guid CharacterId);
public record SaveCharacterStateCommand(Guid CharacterId);
public record LoadCharacterStateCommand(Guid CharacterId);