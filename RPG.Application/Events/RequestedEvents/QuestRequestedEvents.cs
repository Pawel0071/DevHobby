using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;

namespace RPG.Application.Events.RequestedEvents;

public sealed record QuestAcceptRequestedEvent(
    EventMetadata Meta,
    Guid CharacterId,
    Guid QuestId
) : IGameEventWithMetadata
{
    public object? Payload => new { CharacterId, QuestId };
    public string? PayloadType => "QuestAcceptRequested";
}

public sealed record QuestCompleteRequestedEvent(
    EventMetadata Meta,
    Guid CharacterId,
    Guid QuestId
) : IGameEventWithMetadata
{
    public object? Payload => new { CharacterId, QuestId };
    public string? PayloadType => "QuestCompleteRequested";
}

public sealed record QuestProgressUpdateRequestedEvent(
    EventMetadata Meta,
    Guid CharacterId,
    Guid QuestId,
    string ObjectiveType,
    int Progress
) : IGameEventWithMetadata
{
    public object? Payload => new { CharacterId, QuestId, ObjectiveType, Progress };
    public string? PayloadType => "QuestProgressUpdateRequested";
}

