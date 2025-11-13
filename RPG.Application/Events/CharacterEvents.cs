using System.Numerics;
using RPG.Abstractions.Interfaces;
using RPG.Domain.Enums;
using RPG.Domain.Models;
using RPG.Domain.Models.Items;

namespace RPG.Application.Events;

public record ItemEquippedEvent(EventMetadata Meta, Guid CharacterId, EquipmentSlot Slot, Item Item) : IGameEventWithMetadata
{ public object? Payload => new { Slot, Item }; public string? PayloadType => "ItemEquip"; }
public record ItemUnequippedEvent(EventMetadata Meta, Guid CharacterId, EquipmentSlot Slot, Item Item) : IGameEventWithMetadata
{ public object? Payload => new { Slot, Item }; public string? PayloadType => "ItemUnequip"; }
public record InventoryFullEvent(EventMetadata Meta, Guid CharacterId, Item Item) : IGameEventWithMetadata
{ public object? Payload => Item; public string? PayloadType => "InventoryFull"; }
public record ItemUsedEvent(EventMetadata Meta, Guid CharacterId, Item Item) : IGameEventWithMetadata
{ public object? Payload => Item; public string? PayloadType => "ItemUsed"; }
public record ItemPutToBankEvent(EventMetadata Meta, Guid CharacterId, Item Item) : IGameEventWithMetadata
{ public object? Payload => Item; public string? PayloadType => "ItemPutBank"; }
public record ItemGottenFromBankEvent(EventMetadata Meta, Guid CharacterId, Item Item) : IGameEventWithMetadata
{ public object? Payload => Item; public string? PayloadType => "ItemGetBank"; }
public record ItemPickupEvent(EventMetadata Meta, Guid CharacterId, Item Item) : IGameEventWithMetadata
{ public object? Payload => Item; public string? PayloadType => "ItemPickup"; }
public record ItemDroppedEvent(EventMetadata Meta, Guid CharacterId, Item Item) : IGameEventWithMetadata
{ public object? Payload => Item; public string? PayloadType => "ItemDrop"; }
public record CharacterMovedEvent(EventMetadata Meta, Guid CharacterId, Location Location) : IGameEventWithMetadata
{ public object? Payload => Location; public string? PayloadType => "CharacterMoved"; }
public record CharacterMovementStoppedEvent(EventMetadata Meta, Guid CharacterId, Location Location) : IGameEventWithMetadata
{ public object? Payload => Location; public string? PayloadType => "CharacterMovementStopped"; }
public record CharacterRotationStartedEvent(EventMetadata Meta, Guid CharacterId, float Rotation, Location Location) : IGameEventWithMetadata
{ public object? Payload => new { Rotation, Location }; public string? PayloadType => "CharacterRotationStarted"; }
public record CharacterRotationStoppedEvent(EventMetadata Meta, Guid CharacterId, float Rotation, Location Location) : IGameEventWithMetadata
{ public object? Payload => new { Rotation, Location }; public string? PayloadType => "CharacterRotationStopped"; }
public record CharacterCreatedEvent(EventMetadata Meta, Guid CharacterId, string Name, Guid PlayerId, Guid SessionId, CharacterClass Class) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, Name, PlayerId, SessionId, Class }; public string? PayloadType => "CharacterCreated"; }
public record ExperienceGainedEvent(EventMetadata Meta, Guid CharacterId, long Amount, long NewTotal, long NewToNext) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, Amount, NewTotal, NewToNext }; public string? PayloadType => "ExperienceGained"; }
public record CharacterLeveledUpEvent(EventMetadata Meta, Guid CharacterId, int NewLevel) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, NewLevel }; public string? PayloadType => "CharacterLeveledUp"; }
public record MovementStartRequestedEvent(EventMetadata Meta, Guid CharacterId, int Direction, bool PreserveFacing) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, Direction, PreserveFacing }; public string? PayloadType => "MovementStartRequested"; }
public record MovementStopRequestedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "MovementStopRequested"; }
public record RotationStartRequestedEvent(EventMetadata Meta, Guid CharacterId, int Direction) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, Direction }; public string? PayloadType => "RotationStartRequested"; }
public record RotationStopRequestedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "RotationStopRequested"; }
public record ItemPickupRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid ItemId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, ItemId }; public string? PayloadType => "ItemPickupRequested"; }
public record ItemEquipRequestedEvent(EventMetadata Meta, Guid CharacterId, EquipmentSlot Slot, Guid ItemId) : IGameEventWithMetadata
{ public object? Payload => new { Slot, ItemId }; public string? PayloadType => "ItemEquipRequested"; }
public record ItemUnequipRequestedEvent(EventMetadata Meta, Guid CharacterId, EquipmentSlot Slot) : IGameEventWithMetadata
{ public object? Payload => new { Slot }; public string? PayloadType => "ItemUnequipRequested"; }
public record PutItemToBankRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid ItemId) : IGameEventWithMetadata
{ public object? Payload => new { ItemId }; public string? PayloadType => "PutItemToBankRequested"; }
public record GetItemFromBankRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid ItemId) : IGameEventWithMetadata
{ public object? Payload => new { ItemId }; public string? PayloadType => "GetItemFromBankRequested"; }
public record UseItemRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid ItemId) : IGameEventWithMetadata
{ public object? Payload => new { ItemId }; public string? PayloadType => "UseItemRequested"; }
public record DropItemRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid ItemId) : IGameEventWithMetadata
{ public object? Payload => new { ItemId }; public string? PayloadType => "DropItemRequested"; }
// ===== Skill requested events =====
public record SkillUseRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid SkillId, Vector3 TargetPosition) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, SkillId, TargetPosition }; public string? PayloadType => "SkillUseRequested"; } // TODO: implement HostedService processing
public record SkillLearnRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid SkillId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, SkillId }; public string? PayloadType => "SkillLearnRequested"; } // TODO: implement HostedService processing
public record SkillLevelUpRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid SkillId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, SkillId }; public string? PayloadType => "SkillLevelUpRequested"; } // TODO: implement HostedService processing
public record SkillUnlearnRequestedEvent(EventMetadata Meta, Guid CharacterId, Guid SkillId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, SkillId }; public string? PayloadType => "SkillUnlearnRequested"; } // TODO: implement HostedService processing

// ===== Skill final events =====
public record SkillUsedEvent(EventMetadata Meta, Guid CharacterId, Guid SkillId, Vector3 TargetPosition) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, SkillId, TargetPosition }; public string? PayloadType => "SkillUsed"; }
public record SkillLearnedEvent(EventMetadata Meta, Guid CharacterId, Guid SkillId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, SkillId }; public string? PayloadType => "SkillLearned"; }
public record SkillLeveledUpEvent(EventMetadata Meta, Guid CharacterId, Guid SkillId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, SkillId }; public string? PayloadType => "SkillLeveledUp"; }
public record SkillUnlearnedEvent(EventMetadata Meta, Guid CharacterId, Guid SkillId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, SkillId }; public string? PayloadType => "SkillUnlearned"; }

// ===== Session/state requested events =====
public record CharacterLoginRequestedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "CharacterLoginRequested"; } // TODO
public record CharacterLogoutRequestedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "CharacterLogoutRequested"; } // TODO
public record CharacterDieRequestedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "CharacterDieRequested"; } // TODO

// ===== Session/state final events =====
public record CharacterLoggedInEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "CharacterLoggedIn"; }
public record CharacterLoggedOutEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "CharacterLoggedOut"; }
public record CharacterDiedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "CharacterDied"; }
public record ExperienceGainRequestedEvent(EventMetadata Meta, Guid CharacterId, long Amount) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId, Amount }; public string? PayloadType => "ExperienceGainRequested"; } // TODO: implement HostedService processing
public record CharacterLevelUpRequestedEvent(EventMetadata Meta, Guid CharacterId) : IGameEventWithMetadata
{ public object? Payload => new { CharacterId }; public string? PayloadType => "CharacterLevelUpRequested"; } // TODO: implement HostedService processing
public record CharacterCreateRequestedEvent(EventMetadata Meta, RPG.Domain.Models.Character Character) : IGameEventWithMetadata
{ public object? Payload => new { Character = Character }; public string? PayloadType => "CharacterCreateRequested"; }
