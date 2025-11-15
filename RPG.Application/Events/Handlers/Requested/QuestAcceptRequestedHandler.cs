using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Events.RequestedEvents;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Models;
using RPG.Domain.Models.Quests;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Events.Handlers.Requested;

public sealed class QuestAcceptRequestedHandler : IRequestedEventHandler
{
    private readonly IQuestService _questService;
    private readonly IModelRepository _repository;
    private readonly IGameStateBroadcaster _broadcaster;

    public QuestAcceptRequestedHandler(
        IQuestService questService,
        IModelRepository repository,
        IGameStateBroadcaster broadcaster)
    {
        _questService = questService;
        _repository = repository;
        _broadcaster = broadcaster;
    }

    public Type EventType => typeof(QuestAcceptRequestedEvent);
    public bool CanHandle(IGameEvent evt) => evt is QuestAcceptRequestedEvent;

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct)
    {
        if (evt is not QuestAcceptRequestedEvent acceptEvent) return;

        var character = await _repository.GetByIdAsync<Character>(acceptEvent.CharacterId, ct).ConfigureAwait(false);
        if (character == null) return;

        var quest = await _repository.GetByIdAsync<Quest>(acceptEvent.QuestId, ct).ConfigureAwait(false);
        if (quest == null) return;

        // Accept quest through service
        var result = _questService.AcceptQuest(character, quest);
        if (!result.Success) return;

        // Save updated character
        await _repository.UpsertAsync(character, ct).ConfigureAwait(false);

        // Broadcast quest accepted delta
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
