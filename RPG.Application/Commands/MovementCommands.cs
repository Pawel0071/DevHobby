// ...existing usings removed...
using System.Numerics;
using RPG.Abstractions.Interfaces;
using RPG.Application.Interfaces;

namespace RPG.Application.Commands;

public record StartMovementCommand(Guid CharacterId, int Direction, bool PreserveFacing = false) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

public record StopMovementCommand(Guid CharacterId) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

public record StartRotationCommand(Guid CharacterId, int Direction) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

public record StopRotationCommand(Guid CharacterId) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

