using System.Numerics;
using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;

namespace RPG.Core.Application.Events;

public record ItemEquippedEvent(Guid CharacterId, EquipmentSlot Slot, Item Item);
public record ItemUnequippedEvent(Guid CharacterId, EquipmentSlot Slot, Item Item);
public record InventoryFullEvent(Guid CharacterId, Item Item);
public record ItemUsedEvent(Guid CharacterId, Item Item);
public record ItemPutToBankEvent(Guid CharacterId, Item Item);
public record ItemGottenFromBankEvent(Guid CharacterId, Item Item);

public record ItemPickupEvent(Guid CharacterId, Item Item);

public record ItemDroppedEvent(Guid CharacterId, Item Item);

public record StartMovementCommand(Guid CharacterId, int Direction);

public record StopMovementCommand(Guid CharacterId);

public record StartRotationCommand(Guid CharacterId, int Direction);

public record StopRotationCommand(Guid CharacterId);