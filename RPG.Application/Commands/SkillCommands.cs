using RPG.Abstractions.Interfaces;
using RPG.Application.Interfaces;
using RPG.Domain.Models.Skills;

namespace RPG.Application.Commands;

public record UseSkillCommand(
    Guid CharacterId,
    Guid SkillId,
    Guid? TargetId
) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

public record LearnSkillCommand(
    Guid CharacterId,
    Skill Skill
) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

public record LevelUpSkillCommand(
    Guid CharacterId,
    Guid SkillId
) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

public record UnlearnSkillCommand(
    Guid CharacterId,
    Guid SkillId
) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

