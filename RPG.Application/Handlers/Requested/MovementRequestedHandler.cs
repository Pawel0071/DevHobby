// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Handlers/Requested/MovementRequestedHandler.cs
using RPG.Abstractions.Interfaces;
using RPG.Application.Events;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Enums;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;
using System.Numerics;

namespace RPG.Application.Handlers.Requested;

public sealed class MovementRequestedHandler : IRequestedEventHandler
{
    private readonly IModelRepository _repository;
    private readonly IMovementService _movementService;
    private readonly IGameEventDispatcher _dispatcher;
    private readonly ILogger<MovementRequestedHandler> _logger;

    public MovementRequestedHandler(IModelRepository repository,
        IMovementService movementService,
        IGameEventDispatcher dispatcher,
        ILogger<MovementRequestedHandler> logger)
    {
        _repository = repository;
        _movementService = movementService;
        _dispatcher = dispatcher;
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
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(new CharacterMovedEvent(req.Meta, req.CharacterId, character.CurrentLocation), ct);
    }

    private async Task HandleStopAsync(MovementStopRequestedEvent req, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(req.CharacterId, ct);
        if (character == null) return;
        var result = _movementService.Stop(character);
        if (!result.Success) return;
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(new CharacterMovementStoppedEvent(req.Meta, req.CharacterId, result.Result ?? character.CurrentLocation), ct);
    }

    private async Task HandleRotationStartAsync(RotationStartRequestedEvent req, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(req.CharacterId, ct);
        if (character == null) return;
        if (!TryGetDirectionVector(req.Direction, out var dir)) return;
        var result = _movementService.Rotate(character, dir);
        if (!result.Success) return;
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(new CharacterRotationStartedEvent(req.Meta, req.CharacterId, result.Result, character.CurrentLocation), ct);
    }

    private async Task HandleRotationStopAsync(RotationStopRequestedEvent req, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(req.CharacterId, ct);
        if (character == null) return;
        var result = _movementService.StopRotation(character);
        if (!result.Success) return;
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(new CharacterRotationStoppedEvent(req.Meta, req.CharacterId, result.Result, character.CurrentLocation), ct);
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

