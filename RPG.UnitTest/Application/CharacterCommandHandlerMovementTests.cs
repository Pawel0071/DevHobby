using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RPG.Application.Commands;
using RPG.Application.Events;
using RPG.Application.Handlers;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Core.Services.MovementService;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Interfaces;
using DomainCharacterRepository = RPG.Domain.Interfaces.ICharacterRepository;
using Xunit;

namespace RPG.UnitTest.Application;

public class CharacterCommandHandlerMovementTests
{
    private readonly Mock<DomainCharacterRepository> _characterRepository = new();
    private readonly Mock<IGameEventDispatcher> _eventDispatcher = new();
    private readonly MovementService _movementService;
    private readonly CharacterCommandHandler _handler;

    public CharacterCommandHandlerMovementTests()
    {
        var inventoryService = new Mock<IInventoryService>();
        var statsService = new Mock<IStatsService>();
        var movementLogger = new Mock<ILogger<MovementService>>();
        _movementService = new MovementService(movementLogger.Object);

        _handler = new CharacterCommandHandler(
            _characterRepository.Object,
            inventoryService.Object,
            Mock.Of<IEquipmentService>(),
            statsService.Object,
            _movementService,
            _eventDispatcher.Object,
            Mock.Of<IDictionaryRegistry<TagDefinition>>());

        _eventDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<CharacterMovedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<CharacterMovementStoppedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<CharacterRotationStartedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<CharacterRotationStoppedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task HandleAsync_StartMovement_ShouldMoveCharacterAndDispatchEvent()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "Mover"
        };
        character.SetCurrentLocation(Location.Create(Vector3.Zero, Guid.NewGuid()));
        character.ModifiedStats[StatsProperty.MoveSpeed] = 5;

        _characterRepository.Setup(repo => repo.GetByIdAsync(character.Id)).ReturnsAsync(character);
        _characterRepository.Setup(repo => repo.SaveAsync(character)).Returns(Task.CompletedTask).Verifiable();

        var command = new StartMovementCommand(character.Id, 1);

        var result = await _handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        character.CurrentLocation.Position.Z.Should().BeApproximately(5f, 0.0001f);
        _characterRepository.Verify(repo => repo.SaveAsync(character), Times.Once);
        _eventDispatcher.Verify(
            dispatcher => dispatcher.DispatchAsync(It.Is<CharacterMovedEvent>(e =>
                e.CharacterId == character.Id &&
                e.Location == character.CurrentLocation), It.IsAny<CancellationToken>()),
            Times.Once);
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

        _characterRepository.Setup(repo => repo.GetByIdAsync(character.Id)).ReturnsAsync(character);

        var command = new StartMovementCommand(character.Id, 0);

        var result = await _handler.HandleAsync(command);

        result.Success.Should().BeFalse();
        _characterRepository.Verify(repo => repo.SaveAsync(It.IsAny<Character>()), Times.Never);
        _eventDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(It.IsAny<CharacterMovedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        character.CurrentLocation.Position.Should().Be(Vector3.Zero);
    }

    [Fact]
    public async Task HandleAsync_StopMovement_ShouldDispatchEvent()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "Stopper"
        };
        var location = Location.Create(new Vector3(2, 0, 3), Guid.NewGuid());
        character.SetCurrentLocation(location);

        _characterRepository.Setup(repo => repo.GetByIdAsync(character.Id)).ReturnsAsync(character);

        var command = new StopMovementCommand(character.Id);

        var result = await _handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        _eventDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(It.Is<CharacterMovementStoppedEvent>(e =>
            e.CharacterId == character.Id &&
            ReferenceEquals(e.Location, location)), It.IsAny<CancellationToken>()), Times.Once);
        _characterRepository.Verify(repo => repo.SaveAsync(It.IsAny<Character>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_StartRotation_ShouldPersistRotationAndDispatchEvent()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Mage)
        {
            Id = Guid.NewGuid(),
            Name = "Spinner"
        };
        var location = Location.Create(Vector3.Zero, Guid.NewGuid());
        character.SetCurrentLocation(location);

        _characterRepository.Setup(repo => repo.GetByIdAsync(character.Id)).ReturnsAsync(character);
        _characterRepository.Setup(repo => repo.SaveAsync(character)).Returns(Task.CompletedTask).Verifiable();

        var command = new StartRotationCommand(character.Id, 3);

        var result = await _handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        character.CurrentLocation.Rotation.Should().BeApproximately(90f, 0.0001f);
        _characterRepository.Verify(repo => repo.SaveAsync(character), Times.Once);
        _eventDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(It.Is<CharacterRotationStartedEvent>(e =>
            e.CharacterId == character.Id &&
            Math.Abs(e.Rotation - character.CurrentLocation.Rotation) < 0.0001f &&
            ReferenceEquals(e.Location, location)), It.IsAny<CancellationToken>()), Times.Once);
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

        _characterRepository.Setup(repo => repo.GetByIdAsync(character.Id)).ReturnsAsync(character);

        var command = new StartRotationCommand(character.Id, 0);

        var result = await _handler.HandleAsync(command);

        result.Success.Should().BeFalse();
        _characterRepository.Verify(repo => repo.SaveAsync(It.IsAny<Character>()), Times.Never);
        _eventDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(It.IsAny<CharacterRotationStartedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_StopRotation_ShouldDispatchEvent()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Mage)
        {
            Id = Guid.NewGuid(),
            Name = "Drifter"
        };
        var location = Location.Create(Vector3.Zero, Guid.NewGuid());
        location.Rotation = 135f;
        character.SetCurrentLocation(location);

        _characterRepository.Setup(repo => repo.GetByIdAsync(character.Id)).ReturnsAsync(character);

        var command = new StopRotationCommand(character.Id);

        var result = await _handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        _eventDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(It.Is<CharacterRotationStoppedEvent>(e =>
            e.CharacterId == character.Id &&
            Math.Abs(e.Rotation - 135f) < 0.0001f &&
            ReferenceEquals(e.Location, location)), It.IsAny<CancellationToken>()), Times.Once);
        _characterRepository.Verify(repo => repo.SaveAsync(It.IsAny<Character>()), Times.Never);
    }
}
