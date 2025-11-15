using RPG.Abstractions.Interfaces;
using RPG.Application.Interfaces;

namespace RPG.Application.Commands;

public record AcceptQuestCommand(
    Guid CharacterId,
    Guid QuestId
) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

public record CompleteQuestCommand(
    Guid CharacterId,
    Guid QuestId
) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

public record UpdateQuestProgressCommand(
    Guid CharacterId,
    Guid QuestId,
    string ObjectiveType,
    int Progress
) : IMetadataCommand
{
    public CommandMetadata Metadata { get; set; } = default!;
}

