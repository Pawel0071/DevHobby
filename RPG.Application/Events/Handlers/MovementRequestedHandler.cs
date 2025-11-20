// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Handlers/Requested/MovementRequestedHandler.cs
using RPG.Abstractions.Interfaces;
using RPG.Application.Events;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Enums;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;
using System.Numerics;
using RPG.Domain.Models.Interaction; // dla CharacterStateUpdate

namespace RPG.Application.Events.Handlers;

public sealed class MovementRequestedHandler : IRequestedEventHandler
{
    public Type EventType => typeof(MovementStartRequestedEvent); // grupa ruchu, ale klucz 1:1 na poziomie orchestratora

    private readonly IModelRepository _repository; // zmiana: nienazwany generycznie
    private readonly IMovementService _movementService;
    private readonly IGameEventDispatcher _dispatcher;
    private readonly ICharacterStateBroadcaster _stateBroadcaster;
    private readonly ILogger<MovementRequestedHandler> _logger;

    public MovementRequestedHandler(
        IModelRepository repository,
        IMovementService movementService,
        IGameEventDispatcher dispatcher,
        ICharacterStateBroadcaster stateBroadcaster,
        ILogger<MovementRequestedHandler> logger)
    {
        _repository = repository;
        _movementService = movementService;
        _dispatcher = dispatcher;
        _stateBroadcaster = stateBroadcaster;
        _logger = logger;
    }

    public bool CanHandle(IGameEvent evt) => evt is MovementStartRequestedEvent or MovementStopRequestedEvent or RotationStartRequestedEvent or RotationStopRequestedEvent;

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct)
    {
        switch (evt)
        {
            case MovementStartRequestedEvent s:
                await HandleStartAsync(s, ct); break;
            case MovementStopRequestedEvent st:
                await HandleStopAsync(st, ct); break;
            case RotationStartRequestedEvent rs:
                await HandleRotationStartAsync(rs, ct); break;
            case RotationStopRequestedEvent rr:
                await HandleRotationStopAsync(rr, ct); break;
        }
    }

    private async Task HandleStartAsync(MovementStartRequestedEvent req, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(req.CharacterId, ct);
        if (character == null) return;
        if (!character.ModifiedStats.TryGetValue(StatsProperty.MoveSpeed, out var speed) || speed <= 0) return;
        if (!TryGetDirectionVector(req.Direction, out var dir)) return;
        var result = _movementService.Move(character, dir, 0.1f, preserveFacing: req.PreserveFacing);
        if (!result.Success) return;
        await _repository.UpsertAsync<Character>(character, ct);

        // bezpośredni broadcast stanu ruchu do klientów
        var update = new CharacterStateUpdate(
            req.CharacterId,
            character.Class,
            character.CurrentLocation,
            IsMoving: true,
            IsRotating: null,
            Rotation: character.CurrentLocation?.Direction,
            Timestamp: DateTime.UtcNow);
        await _stateBroadcaster.BroadcastAsync(update, ct);
    }

    private async Task HandleStopAsync(MovementStopRequestedEvent req, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(req.CharacterId, ct);
        if (character == null) return;
        var result = _movementService.Stop(character);
        if (!result.Success) return;
        await _repository.UpsertAsync<Character>(character, ct);
        var location = result.Result ?? character.CurrentLocation;

        var update = new CharacterStateUpdate(
            req.CharacterId,
            character.Class,
            location,
            IsMoving: false,
            IsRotating: null,
            Rotation: location?.Direction,
            Timestamp: DateTime.UtcNow);
        await _stateBroadcaster.BroadcastAsync(update, ct);
    }

    private async Task HandleRotationStartAsync(RotationStartRequestedEvent req, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(req.CharacterId, ct);
        if (character == null) return;
        if (!TryGetDirectionVector(req.Direction, out var dir)) return;
        var result = _movementService.Rotate(character, dir);
        if (!result.Success) return;
        await _repository.UpsertAsync<Character>(character, ct);

        var update = new CharacterStateUpdate(
            req.CharacterId,
            character.Class,
            character.CurrentLocation,
            IsMoving: null,
            IsRotating: true,
            Rotation: result.Result,
            Timestamp: DateTime.UtcNow);
        await _stateBroadcaster.BroadcastAsync(update, ct);
    }

    private async Task HandleRotationStopAsync(RotationStopRequestedEvent req, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(req.CharacterId, ct);
        if (character == null) return;
        var result = _movementService.StopRotation(character);
        if (!result.Success) return;
        await _repository.UpsertAsync<Character>(character, ct);

        var update = new CharacterStateUpdate(
            req.CharacterId,
            character.Class,
            character.CurrentLocation,
            IsMoving: null,
            IsRotating: false,
            Rotation: result.Result,
            Timestamp: DateTime.UtcNow);
        await _stateBroadcaster.BroadcastAsync(update, ct);
    }

    private static bool TryGetDirectionVector(int direction, out Vector3 vector)
    {
        vector = direction switch
        {
            1 => new Vector3(0f, 1f, 0f),
            2 => new Vector3(1f, 1f, 0f),
            3 => new Vector3(1f, 0f, 0f),
            4 => new Vector3(1f, -1f, 0f),
            5 => new Vector3(0f, -1f, 0f),
            6 => new Vector3(-1f, -1f, 0f),
            7 => new Vector3(-1f, 0f, 0f),
            8 => new Vector3(-1f, 1f, 0f),
            _ => Vector3.Zero
        };
        return vector != Vector3.Zero;
    }
}
