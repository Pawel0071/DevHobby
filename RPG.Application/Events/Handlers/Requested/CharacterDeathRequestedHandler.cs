using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Application.Events.RequestedEvents;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Events.Handlers.Requested;

public sealed class CharacterDeathRequestedHandler : IRequestedEventHandler
{
    private readonly IModelRepository _repository;
    private readonly IGameStateBroadcaster _broadcaster;
    private readonly ICharacterService _service;
    private readonly ILogger<CharacterDeathRequestedHandler> _logger;

    public CharacterDeathRequestedHandler(
        IModelRepository repository,
        IGameStateBroadcaster broadcaster,
        ICharacterService service,
        ILogger<CharacterDeathRequestedHandler> logger)
    {
        _repository = repository;
        _broadcaster = broadcaster;
        _service = service;
        _logger = logger;
    }

    public Type EventType => typeof(CharacterDeathRequestedEvent);
    public bool CanHandle(IGameEvent evt) => evt is CharacterDeathRequestedEvent;

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct)
    {
        if (evt is not CharacterDeathRequestedEvent deathEvent) return;

        var character = await _repository.GetByIdAsync<Character>(deathEvent.CharacterId, ct).ConfigureAwait(false);
        if (character == null)
        {
            _logger.Warn($"Character {deathEvent.CharacterId} not found for death event");
            return;
        }

        // Call Core Service for death logic
        var result = await _service.HandleDeathAsync(character, ct).ConfigureAwait(false);
        if (!result.Success)
        {
            _logger.Error($"Failed to handle death for character {character.Id}: {result.Message}");
            return;
        }

        // Save updated character
        await _repository.UpsertAsync(character, ct).ConfigureAwait(false);

        // Broadcast death delta
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

