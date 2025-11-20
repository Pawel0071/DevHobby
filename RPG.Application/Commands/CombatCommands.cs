using RPG.Application.Interfaces;

namespace RPG.Application.Commands;

using RPG.Abstractions.Interfaces;
using RPG.Domain.Models;

public sealed record AttackNpcCommand(Guid CharacterId, Guid NpcId) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

public sealed record NpcDamageReportedCommand(Guid NpcId, Guid CharacterId, float DamageAmount) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

public sealed record NpcRespawnCommand(Guid NpcId) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

