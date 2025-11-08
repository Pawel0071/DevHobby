using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Enums;

namespace RPG.Application.Events;

public record ItemEquippedEvent(Guid CharacterId, EquipmentSlot Slot, Item Item);

public record ItemUnequippedEvent(Guid CharacterId, EquipmentSlot Slot, Item Item);

public record InventoryFullEvent(Guid CharacterId, Item Item);

public record ItemUsedEvent(Guid CharacterId, Item Item);

public record ItemPutToBankEvent(Guid CharacterId, Item Item);

public record ItemGottenFromBankEvent(Guid CharacterId, Item Item);

public record ItemPickupEvent(Guid CharacterId, Item Item);

public record ItemDroppedEvent(Guid CharacterId, Item Item);

public record CharacterMovedEvent(Guid CharacterId, Location Location);
