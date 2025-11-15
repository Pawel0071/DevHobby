using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Events.RequestedEvents;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Events.Handlers.Requested;

public sealed class SkillLearnRequestedHandler : IRequestedEventHandler
{
    private readonly ISkillService _skillService;
    private readonly IModelRepository _repository;
    private readonly IGameStateBroadcaster _broadcaster;

    public SkillLearnRequestedHandler(
        ISkillService skillService,
        IModelRepository repository,
        IGameStateBroadcaster broadcaster)
    {
        _skillService = skillService;
        _repository = repository;
        _broadcaster = broadcaster;
    }

    public Type EventType => typeof(SkillLearnRequestedEvent);
    public bool CanHandle(IGameEvent evt) => evt is SkillLearnRequestedEvent;

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct)
    {
        if (evt is not SkillLearnRequestedEvent learnEvent) return;

        var character = await _repository.GetByIdAsync<Character>(learnEvent.CharacterId, ct).ConfigureAwait(false);
        if (character == null) return;

        // Learn skill through service
        var result = _skillService.LearnSkill(character, learnEvent.Skill);
        if (!result.Success) return;

        // Save updated character
        await _repository.UpsertAsync(character, ct).ConfigureAwait(false);

        // Broadcast skill learned delta
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
