using System.Numerics;
using RPG.Abstractions.Interfaces;
using RPG.Application.Interfaces;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Domain.Models;

namespace RPG.Application.Commands;

public record EquipItemCommand(Guid CharacterId, EquipmentSlot Slot, Guid ItemId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record UnequipItemCommand(Guid CharacterId, EquipmentSlot Slot) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record PutItemToBankCommand(Guid CharacterId, Guid ItemId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record GetItemFromBankCommand(Guid CharacterId, Guid ItemId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record UseItemCommand(Guid CharacterId, Guid ItemId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record DropItemCommand(Guid CharacterId, Guid ItemId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record PickUpItemCommand(Guid CharacterId, Guid ItemId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record GainExperienceCommand(Guid CharacterId, int Amount) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record LevelUpCommand(Guid CharacterId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record StartMovementCommand(Guid CharacterId, int Direction, bool PreserveFacing = false) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record StopMovementCommand(Guid CharacterId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record StartRotationCommand(Guid CharacterId, int Direction) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record StopRotationCommand(Guid CharacterId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record UseSkillCommand(Guid CharacterId, Guid SkillId, Vector3 TargetPosition) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record LearnSkillCommand(Guid CharacterId, Guid SkillId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record LevelUpSkillCommand(Guid CharacterId, Guid SkillId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record UnLearnSkillCommand(Guid CharacterId, Guid SkillId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record DieCommand(Guid CharacterId): IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record LoginCharacterCommand(Guid CharacterId): IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record LogoutCharacterCommand(Guid CharacterId): IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

public record CreateCharacterCommand( Character Character ) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }
