using System.Diagnostics;
using RPG.Abstractions.Interfaces;
using RPG.Application.Diagnostics;
using RPG.Application.Events;
using RPG.Application.Interfaces;
using RPG.Domain.Models;
using RPG.Domain.Models.Interaction;

namespace RPG.Application.Handlers;

public class CharacterMovementEventHandler :
    IGameEventHandler<CharacterMovedEvent>,
    IGameEventHandler<CharacterMovementStoppedEvent>,
    IGameEventHandler<CharacterRotationStartedEvent>,
    IGameEventHandler<CharacterRotationStoppedEvent>
{
    private readonly ICharacterStateBroadcaster _stateBroadcaster;

    public CharacterMovementEventHandler(ICharacterStateBroadcaster stateBroadcaster)
    {
        _stateBroadcaster = stateBroadcaster;
    }

    public Task HandleAsync(CharacterMovedEvent gameEvent, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("CharacterMovementEventHandler.CharacterMoved");
        activity?.SetTag("rpg.event.type", nameof(CharacterMovedEvent));
        activity?.SetTag("rpg.character.id", gameEvent.CharacterId);

        var update = new CharacterStateUpdate(
            gameEvent.CharacterId,
            gameEvent.Location,
            IsMoving: true,
            IsRotating: null,
            Rotation: gameEvent.Location?.Rotation,
            Timestamp: DateTime.UtcNow);

        return _stateBroadcaster.BroadcastAsync(update, cancellationToken);
    }

    public Task HandleAsync(CharacterMovementStoppedEvent gameEvent, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("CharacterMovementEventHandler.CharacterMovementStopped");
        activity?.SetTag("rpg.event.type", nameof(CharacterMovementStoppedEvent));
        activity?.SetTag("rpg.character.id", gameEvent.CharacterId);

        var update = new CharacterStateUpdate(
            gameEvent.CharacterId,
            gameEvent.Location,
            IsMoving: false,
            IsRotating: null,
            Rotation: gameEvent.Location?.Rotation,
            Timestamp: DateTime.UtcNow);

        return _stateBroadcaster.BroadcastAsync(update, cancellationToken);
    }

    public Task HandleAsync(CharacterRotationStartedEvent gameEvent, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("CharacterMovementEventHandler.CharacterRotationStarted");
        activity?.SetTag("rpg.event.type", nameof(CharacterRotationStartedEvent));
        activity?.SetTag("rpg.character.id", gameEvent.CharacterId);

        var update = new CharacterStateUpdate(
            gameEvent.CharacterId,
            gameEvent.Location,
            IsMoving: null,
            IsRotating: true,
            Rotation: gameEvent.Rotation,
            Timestamp: DateTime.UtcNow);

        return _stateBroadcaster.BroadcastAsync(update, cancellationToken);
    }

    public Task HandleAsync(CharacterRotationStoppedEvent gameEvent, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("CharacterMovementEventHandler.CharacterRotationStopped");
        activity?.SetTag("rpg.event.type", nameof(CharacterRotationStoppedEvent));
        activity?.SetTag("rpg.character.id", gameEvent.CharacterId);

        var update = new CharacterStateUpdate(
            gameEvent.CharacterId,
            gameEvent.Location,
            IsMoving: null,
            IsRotating: false,
            Rotation: gameEvent.Rotation,
            Timestamp: DateTime.UtcNow);

        return _stateBroadcaster.BroadcastAsync(update, cancellationToken);
    }
}
