using RPG.Abstractions.Interfaces;
using RPG.Application.Interfaces;
using RPG.Domain.Enums;

namespace RPG.Application.Commands;

public record EquipItemCommand(Guid CharacterId, EquipmentSlot Slot, Guid ItemId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }
public record UnequipItemCommand(Guid CharacterId, EquipmentSlot Slot) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }
public record PutItemToBankCommand(Guid CharacterId, Guid ItemId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }
public record GetItemFromBankCommand(Guid CharacterId, Guid ItemId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }
public record UseItemCommand(Guid CharacterId, Guid ItemId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }
public record DropItemCommand(Guid CharacterId, Guid ItemId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }
public record PickUpItemCommand(Guid CharacterId, Guid ItemId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

