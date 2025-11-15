using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Events.RequestedEvents;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Events.Handlers.Requested;

public sealed class ExperienceGainRequestedHandler : IRequestedEventHandler
{
    private readonly ILevelingService _levelingService;
    private readonly IModelRepository _repository;
    private readonly IGameStateBroadcaster _broadcaster;

    public ExperienceGainRequestedHandler(
        ILevelingService levelingService,
        IModelRepository repository,
        IGameStateBroadcaster broadcaster)
    {
        _levelingService = levelingService;
        _repository = repository;
        _broadcaster = broadcaster;
    }

    public Type EventType => typeof(ExperienceGainRequestedEvent);
    public bool CanHandle(IGameEvent evt) => evt is ExperienceGainRequestedEvent;

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct)
    {
        if (evt is not ExperienceGainRequestedEvent xpEvent) return;

        var character = await _repository.GetByIdAsync<Character>(xpEvent.CharacterId, ct).ConfigureAwait(false);
        if (character == null) return;

        // Gain experience through service
        var result = _levelingService.GrantExperience(character, xpEvent.Amount);
        if (!result.Success) return;

        // Save updated character
        await _repository.UpsertAsync(character, ct).ConfigureAwait(false);

        // Broadcast XP gained delta
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
