using System;
using System.Threading;
using System.Threading.Tasks;
using RPG.Application.Events;
using RPG.Application.Interfaces;
using RPG.GameServer.Interfaces;
using RPG.GameServer.Models;

namespace RPG.GameServer.EventHandlers;

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
