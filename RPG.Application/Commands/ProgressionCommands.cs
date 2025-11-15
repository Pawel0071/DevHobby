using RPG.Abstractions.Interfaces;
using RPG.Application.Interfaces;

namespace RPG.Application.Commands;

public record GainExperienceCommand(
    Guid CharacterId,
    int ExperienceAmount,
    string Source
) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

public record LevelUpCommand(
    Guid CharacterId
) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

public record DieCommand(
    Guid CharacterId,
    Guid? KillerId
) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

