using RPG.Abstractions.Interfaces;
using RPG.Domain.Enums;

namespace RPG.Application.Events;

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

