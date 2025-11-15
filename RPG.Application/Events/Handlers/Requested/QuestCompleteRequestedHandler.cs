using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Events.RequestedEvents;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Events.Handlers.Requested;

public sealed class QuestCompleteRequestedHandler : IRequestedEventHandler
{
    private readonly IQuestService _questService;
    private readonly IModelRepository _repository;
    private readonly IGameStateBroadcaster _broadcaster;

    public QuestCompleteRequestedHandler(
        IQuestService questService,
        IModelRepository repository,
        IGameStateBroadcaster broadcaster)
    {
        _questService = questService;
        _repository = repository;
        _broadcaster = broadcaster;
    }

    public Type EventType => typeof(QuestCompleteRequestedEvent);
    public bool CanHandle(IGameEvent evt) => evt is QuestCompleteRequestedEvent;

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct)
    {
        if (evt is not QuestCompleteRequestedEvent completeEvent) return;

        var character = await _repository.GetByIdAsync<Character>(completeEvent.CharacterId, ct).ConfigureAwait(false);
        if (character == null) return;

        // Complete quest through service
        var result = _questService.CompleteQuest(character, completeEvent.QuestId);
        if (!result.Success) return;

        // Save updated character
        await _repository.UpsertAsync(character, ct).ConfigureAwait(false);

        // Broadcast quest completed delta
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

