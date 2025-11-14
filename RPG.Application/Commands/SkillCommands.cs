using System.Numerics;
using RPG.Abstractions.Interfaces;
using RPG.Application.Interfaces;

namespace RPG.Application.Commands;

public record UseSkillCommand(Guid CharacterId, Guid SkillId, Vector3 TargetPosition) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }
public record LearnSkillCommand(Guid CharacterId, Guid SkillId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }
public record LevelUpSkillCommand(Guid CharacterId, Guid SkillId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }
public record UnLearnSkillCommand(Guid CharacterId, Guid SkillId) : IMetadataCommand { public CommandMetadata Metadata { get; set; } = default!; }

