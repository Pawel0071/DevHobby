using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Events.RequestedEvents;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Events.Handlers.Requested;

public sealed class QuestProgressUpdateRequestedHandler : IRequestedEventHandler
{
    private readonly IQuestService _questService;
    private readonly IModelRepository _repository;
    private readonly IGameStateBroadcaster _broadcaster;

    public QuestProgressUpdateRequestedHandler(
        IQuestService questService,
        IModelRepository repository,
        IGameStateBroadcaster broadcaster)
    {
        _questService = questService;
        _repository = repository;
        _broadcaster = broadcaster;
    }

    public Type EventType => typeof(QuestProgressUpdateRequestedEvent);
    public bool CanHandle(IGameEvent evt) => evt is QuestProgressUpdateRequestedEvent;

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct)
    {
        if (evt is not QuestProgressUpdateRequestedEvent progressEvent) return;

        var character = await _repository.GetByIdAsync<Character>(progressEvent.CharacterId, ct).ConfigureAwait(false);
        if (character == null) return;

        // Update quest progress through service
        var result = _questService.UpdateQuestProgress(
            character,
            progressEvent.QuestId,
            progressEvent.ObjectiveType,
            progressEvent.Progress);

        if (!result.Success) return;

        // Save updated character
        await _repository.UpsertAsync(character, ct).ConfigureAwait(false);

        // Broadcast quest progress updated delta
        var delta = new GameDeltaUpdate
        {
            WorldId = character.CurrentLocation?.WorldId ?? Guid.Empty,
            CharacterChanges = new List<CharacterDelta>
            {
                new CharacterDelta
                {
                    CharacterId = character.Id,
                    Location = character.CurrentLocation
                }
            }
        };

        await _broadcaster.BroadcastDeltaAsync(delta, ct).ConfigureAwait(false);
    }
}

