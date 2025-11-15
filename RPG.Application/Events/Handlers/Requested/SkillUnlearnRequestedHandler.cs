using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Events.RequestedEvents;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Events.Handlers.Requested;

public sealed class SkillUnlearnRequestedHandler : IRequestedEventHandler
{
    private readonly ISkillService _skillService;
    private readonly IModelRepository _repository;
    private readonly IGameStateBroadcaster _broadcaster;

    public SkillUnlearnRequestedHandler(
        ISkillService skillService,
        IModelRepository repository,
        IGameStateBroadcaster broadcaster)
    {
        _skillService = skillService;
        _repository = repository;
        _broadcaster = broadcaster;
    }

    public Type EventType => typeof(SkillUnlearnRequestedEvent);
    public bool CanHandle(IGameEvent evt) => evt is SkillUnlearnRequestedEvent;

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct)
    {
        if (evt is not SkillUnlearnRequestedEvent unlearnEvent) return;

        var character = await _repository.GetByIdAsync<Character>(unlearnEvent.CharacterId, ct).ConfigureAwait(false);
        if (character == null) return;

        var skill = character.Skills.FirstOrDefault(s => s.Key.Id == unlearnEvent.SkillId).Key;
        if (skill == null) return;

        // Unlearn skill through service
        var result = _skillService.UnlearnSkill(character, skill);
        if (!result.Success) return;

        // Save updated character
        await _repository.UpsertAsync(character, ct).ConfigureAwait(false);

        // Broadcast skill unlearned delta
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

