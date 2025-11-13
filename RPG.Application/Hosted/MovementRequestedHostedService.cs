using Microsoft.Extensions.Hosting;
using RPG.Abstractions.Interfaces;
using RPG.Application.Events;
using RPG.Infrastructure.Interfaces;
using RPG.Domain.Models;
using RPG.Core.Interfaces;
using System.Numerics;
using RPG.Application.Infrastructure;

namespace RPG.Application.Hosted;

[Obsolete("Replaced by RequestedEventsHostedService. Keeping class for reference; not registered in DI anymore.")]
public sealed class MovementRequestedHostedService : BackgroundService
{
    private readonly IRequestEventQueue _requestQueue;
    private readonly IGameEventDispatcher _dispatcher;
    private readonly IMovementService _movementService;
    private readonly IModelRepository _repository;
    private readonly ILogger<MovementRequestedHostedService> _logger;

    public MovementRequestedHostedService(
        IRequestEventQueue requestQueue,
        IGameEventDispatcher dispatcher,
        IMovementService movementService,
        IModelRepository repository,
        ILogger<MovementRequestedHostedService> logger)
    {
        _requestQueue = requestQueue;
        _dispatcher = dispatcher;
        _movementService = movementService;
        _repository = repository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Info("MovementRequestedHostedService started (DEPRECATED, not used)");
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        _logger.Info("MovementRequestedHostedService stopped");
    }

    private async Task ProcessAsync(IGameEvent evt, CancellationToken ct)
    {
        switch (evt)
        {
            case MovementStartRequestedEvent startReq:
                await HandleStartMovement(startReq, ct);
                break;
            case MovementStopRequestedEvent stopReq:
                await HandleStopMovement(stopReq, ct);
                break;
            case RotationStartRequestedEvent rotStart:
                await HandleStartRotation(rotStart, ct);
                break;
            case RotationStopRequestedEvent rotStop:
                await HandleStopRotation(rotStop, ct);
                break;
        }
    }

    private async Task HandleStartMovement(MovementStartRequestedEvent req, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(req.CharacterId, ct);
        if (character == null) return;
        if (!character.ModifiedStats.TryGetValue(RPG.Domain.Enums.StatsProperty.MoveSpeed, out var speed) || speed <= 0) return;
        if (!TryGetDirectionVector(req.Direction, out var dir)) return;
        var result = _movementService.Move(character, dir, 0.1f, preserveFacing: req.PreserveFacing);
        if (!result.Success) return;
        var moved = new CharacterMovedEvent(req.Meta, req.CharacterId, character.CurrentLocation);
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(moved, ct);
    }

    private async Task HandleStopMovement(MovementStopRequestedEvent req, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(req.CharacterId, ct);
        if (character == null) return;
        var result = _movementService.Stop(character);
        if (!result.Success) return;
        var stopped = new CharacterMovementStoppedEvent(req.Meta, req.CharacterId, result.Result ?? character.CurrentLocation);
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(stopped, ct);
    }

    private async Task HandleStartRotation(RotationStartRequestedEvent req, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(req.CharacterId, ct);
        if (character == null) return;
        if (!TryGetDirectionVector(req.Direction, out var dir)) return;
        var result = _movementService.Rotate(character, dir);
        if (!result.Success) return;
        var started = new CharacterRotationStartedEvent(req.Meta, req.CharacterId, result.Result, character.CurrentLocation);
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(started, ct);
    }

    private async Task HandleStopRotation(RotationStopRequestedEvent req, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(req.CharacterId, ct);
        if (character == null) return;
        var result = _movementService.StopRotation(character);
        if (!result.Success) return;
        var stopped = new CharacterRotationStoppedEvent(req.Meta, req.CharacterId, result.Result, character.CurrentLocation);
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(stopped, ct);
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
