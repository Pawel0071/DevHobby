using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Events.RequestedEvents;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Events.Handlers.Requested;

public sealed class SkillUsageRequestedHandler : IRequestedEventHandler
{
    private readonly ISkillService _skillService;
    private readonly IModelRepository _repository;
    private readonly IGameStateBroadcaster _broadcaster;

    public SkillUsageRequestedHandler(
        ISkillService skillService,
        IModelRepository repository,
        IGameStateBroadcaster broadcaster)
    {
        _skillService = skillService;
        _repository = repository;
        _broadcaster = broadcaster;
    }

    public Type EventType => typeof(SkillUsageRequestedEvent);
    public bool CanHandle(IGameEvent evt) => evt is SkillUsageRequestedEvent;

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct)
    {
        if (evt is not SkillUsageRequestedEvent skillEvent) return;

        var character = await _repository.GetByIdAsync<Character>(skillEvent.CharacterId, ct).ConfigureAwait(false);
        if (character == null) return;

        var skill = character.Skills.FirstOrDefault(s => s.Key.Id == skillEvent.SkillId).Key;
        if (skill == null) return;

        // Use skill through service
        var result = _skillService.UseSkill(character, skill);
        if (!result.Success) return;

        // Save updated character
        await _repository.UpsertAsync(character, ct).ConfigureAwait(false);

        // Broadcast skill usage delta
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
