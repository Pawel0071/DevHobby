using System.Numerics;
using FluentAssertions;
using Moq;
using RPG.Abstractions.Interfaces;
using RPG.Application.Commands;
using RPG.Application.Commands.Handlers;
using RPG.Application.Events;
using RPG.Application.Infrastructure;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Core.Services.MovementService;
using RPG.Domain.Common;
using RPG.Domain.Enums;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.UnitTest.Application;

public class CharacterCommandHandlerMovementTests
{
    private readonly Mock<IModelRepository> _characterRepository = new();
    private readonly Mock<IGameEventDispatcher> _eventDispatcher = new();
    private readonly CommandHandler _handler;
    private readonly IEventSequenceStore _sequenceStore = new InMemoryEventSequenceStore();
    private readonly Mock<IRequestEventQueue> _requestQueue = new();
    private readonly Mock<IRequestedEventInlineDispatcher> _inlineDispatcher = new();

    public CharacterCommandHandlerMovementTests()
    {
        _inlineDispatcher
            .Setup(d => d.TryHandleAsync(It.IsAny<IGameEventWithMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new CommandHandler(
            _requestQueue.Object,
            new DeterministicEventIdProvider(),
            _sequenceStore,
            _inlineDispatcher.Object);

        _eventDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<MovementStartRequestedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<MovementStopRequestedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<RotationStartRequestedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<RotationStopRequestedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task HandleAsync_StartMovement_ShouldPublishRequestedEvent()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "Mover"
        };
        character.SetCurrentLocation(Location.Create(Vector3.Zero, Guid.NewGuid()));
        character.ModifiedStats[StatsProperty.MoveSpeed] = 5;

        _characterRepository.Setup(repo => repo.GetByIdAsync<Character>(character.Id, It.IsAny<CancellationToken>())).ReturnsAsync(character);

        var command = new StartMovementCommand(character.Id, 1);

        var result = await _handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        _requestQueue.Verify(q => q.Enqueue(It.Is<MovementStartRequestedEvent>(e => e.CharacterId == character.Id && e.Direction == 1)), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_StartMovement_WithInvalidDirection_ShouldFail()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Mage)
        {
            Id = Guid.NewGuid(),
            Name = "Static"
        };
        character.SetCurrentLocation(Location.Create(Vector3.Zero, Guid.NewGuid()));
        character.ModifiedStats[StatsProperty.MoveSpeed] = 5;

        _characterRepository.Setup(repo => repo.GetByIdAsync<Character>(character.Id, It.IsAny<CancellationToken>())).ReturnsAsync(character);

        var command = new StartMovementCommand(character.Id, 0);

        var result = await _handler.HandleAsync(command);

        result.Success.Should().BeFalse();
        _eventDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(It.IsAny<MovementStartRequestedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        character.CurrentLocation.Position.Should().Be(Vector3.Zero);
    }

    [Fact]
    public async Task HandleAsync_StopMovement_ShouldPublishRequestedEvent()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "Stopper"
        };
        var location = Location.Create(new Vector3(2, 0, 3), Guid.NewGuid());
        character.SetCurrentLocation(location);

        _characterRepository.Setup(repo => repo.GetByIdAsync<Character>(character.Id, It.IsAny<CancellationToken>())).ReturnsAsync(character);

        var command = new StopMovementCommand(character.Id);

        var result = await _handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        _requestQueue.Verify(q => q.Enqueue(It.Is<MovementStopRequestedEvent>(e => e.CharacterId == character.Id)), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_StartRotation_ShouldPublishRequestedEvent()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Mage)
        {
            Id = Guid.NewGuid(),
            Name = "Spinner"
        };
        var location = Location.Create(Vector3.Zero, Guid.NewGuid());
        character.SetCurrentLocation(location);

        _characterRepository.Setup(repo => repo.GetByIdAsync<Character>(character.Id, It.IsAny<CancellationToken>())).ReturnsAsync(character);

        var command = new StartRotationCommand(character.Id, 3);

        var result = await _handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        _requestQueue.Verify(q => q.Enqueue(It.Is<RotationStartRequestedEvent>(e => e.CharacterId == character.Id && e.Direction == 3)), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_StartRotation_WithInvalidDirection_ShouldFail()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "Confused"
        };
        character.SetCurrentLocation(Location.Create(Vector3.Zero, Guid.NewGuid()));

        _characterRepository.Setup(repo => repo.GetByIdAsync<Character>(character.Id, It.IsAny<CancellationToken>())).ReturnsAsync(character);

        var command = new StartRotationCommand(character.Id, 0);

        var result = await _handler.HandleAsync(command);

        result.Success.Should().BeFalse();
        _eventDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(It.IsAny<RotationStartRequestedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_StopRotation_ShouldPublishRequestedEvent()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Mage)
        {
            Id = Guid.NewGuid(),
            Name = "Drifter"
        };
        var location = Location.Create(Vector3.Zero, Guid.NewGuid());
        location.Rotation = 135f;
        character.SetCurrentLocation(location);

        _characterRepository.Setup(repo => repo.GetByIdAsync<Character>(character.Id, It.IsAny<CancellationToken>())).ReturnsAsync(character);

        var command = new StopRotationCommand(character.Id);

        var result = await _handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        _requestQueue.Verify(q => q.Enqueue(It.Is<RotationStopRequestedEvent>(e => e.CharacterId == character.Id)), Times.Once);
    }
}
