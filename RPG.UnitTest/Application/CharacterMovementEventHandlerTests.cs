using System.Numerics;
using Moq;
using RPG.Abstractions.Interfaces;
using RPG.Application.Events;
using RPG.Application.Handlers;
using RPG.Domain.Entities;
using RPG.Domain.Models;

namespace RPG.UnitTest.Application;

public class CharacterMovementEventHandlerTests
{
    private readonly Mock<ICharacterStateBroadcaster> _broadcaster = new();
    private readonly CharacterMovementEventHandler _handler;

    public CharacterMovementEventHandlerTests()
    {
        _handler = new CharacterMovementEventHandler(_broadcaster.Object);
        _broadcaster
            .Setup(b => b.BroadcastAsync(It.IsAny<CharacterStateUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task HandleAsync_MoveEvent_ShouldBroadcastMovingState()
    {
        var location = Location.Create(new Vector3(2, 0, 1), Guid.NewGuid());
        location.Rotation = 45f;
        var gameEvent = new CharacterMovedEvent(Guid.NewGuid(), location);

        await _handler.HandleAsync(gameEvent, CancellationToken.None);

        _broadcaster.Verify(b => b.BroadcastAsync(
            It.Is<CharacterStateUpdate>(update =>
                update.CharacterId == gameEvent.CharacterId &&
                update.IsMoving == true &&
                update.IsRotating == null &&
                update.Location == location &&
                Math.Abs((update.Rotation ?? 0f) - location.Rotation) < 0.001f),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_StopMoveEvent_ShouldBroadcastStoppedState()
    {
        var location = Location.Create(Vector3.Zero, Guid.NewGuid());
        var gameEvent = new CharacterMovementStoppedEvent(Guid.NewGuid(), location);

        await _handler.HandleAsync(gameEvent, CancellationToken.None);

        _broadcaster.Verify(b => b.BroadcastAsync(
            It.Is<CharacterStateUpdate>(update =>
                update.CharacterId == gameEvent.CharacterId &&
                update.IsMoving == false &&
                update.IsRotating == null &&
                update.Location == location),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_StartRotation_ShouldBroadcastRotatingState()
    {
        var location = Location.Create(Vector3.Zero, Guid.NewGuid());
        var gameEvent = new CharacterRotationStartedEvent(Guid.NewGuid(), 120f, location);

        await _handler.HandleAsync(gameEvent, CancellationToken.None);

        _broadcaster.Verify(b => b.BroadcastAsync(
            It.Is<CharacterStateUpdate>(update =>
                update.CharacterId == gameEvent.CharacterId &&
                update.IsMoving == null &&
                update.IsRotating == true &&
                Math.Abs((update.Rotation ?? 0f) - 120f) < 0.001f),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_StopRotation_ShouldBroadcastIdleRotationState()
    {
        var location = Location.Create(Vector3.Zero, Guid.NewGuid());
        var gameEvent = new CharacterRotationStoppedEvent(Guid.NewGuid(), 30f, location);

        await _handler.HandleAsync(gameEvent, CancellationToken.None);

        _broadcaster.Verify(b => b.BroadcastAsync(
            It.Is<CharacterStateUpdate>(update =>
                update.CharacterId == gameEvent.CharacterId &&
                update.IsMoving == null &&
                update.IsRotating == false &&
                Math.Abs((update.Rotation ?? 0f) - 30f) < 0.001f),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
