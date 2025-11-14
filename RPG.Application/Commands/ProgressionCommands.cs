using RPG.Abstractions.Interfaces;
using RPG.Application.Interfaces;

namespace RPG.Application.Commands;

public record GainExperienceCommand(Guid CharacterId, int Amount) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }
public record LevelUpCommand(Guid CharacterId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }
public record DieCommand(Guid CharacterId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

