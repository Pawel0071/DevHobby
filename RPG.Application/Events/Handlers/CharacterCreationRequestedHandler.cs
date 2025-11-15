using RPG.Abstractions.Interfaces;
using RPG.Application.Interfaces;
using RPG.Infrastructure.Interfaces;
using RPG.Domain.Enums;
using RPG.Domain.Models;

namespace RPG.Application.Events.Handlers;

public sealed class CharacterCreationRequestedHandler : IRequestedEventHandler
{
    public Type EventType => typeof(CharacterCreateRequestedEvent);

    private readonly IModelRepository _repository;
    private readonly IGameEventDispatcher _dispatcher;
    private readonly ILogger<CharacterCreationRequestedHandler> _logger;

    public CharacterCreationRequestedHandler(
        IModelRepository repository,
        IGameEventDispatcher dispatcher,
        ILogger<CharacterCreationRequestedHandler> logger)
    {
        _repository = repository;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public bool CanHandle(IGameEvent evt) => evt is CharacterCreateRequestedEvent;

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct)
    {
        var e = (CharacterCreateRequestedEvent)evt;
        var character = e.Character;

        // Ensure ModifiedStats has at least BaseStats values (so movement can use MoveSpeed etc.)
        if (character.BaseStats.Count > 0)
        {
            foreach (var kv in character.BaseStats)
            {
                if (!character.ModifiedStats.ContainsKey(kv.Key))
                {
                    character.ModifiedStats[kv.Key] = kv.Value;
                }
            }
        }
        // Ensure MoveSpeed default if still missing
        if (!character.ModifiedStats.ContainsKey(StatsProperty.MoveSpeed))
        {
            character.ModifiedStats[StatsProperty.MoveSpeed] = character.BaseStats.TryGetValue(StatsProperty.MoveSpeed, out var ms) ? ms : 5;
        }

        await _repository.UpsertAsync(character, ct);
        // Finalny CharacterCreatedEvent został usunięty – stan reprezentuje zapisany model.
    }
}
