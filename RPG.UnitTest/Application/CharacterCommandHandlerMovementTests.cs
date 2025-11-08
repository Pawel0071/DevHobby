using System.Numerics;
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

namespace RPG.UnitTest.Application;

public class CharacterCommandHandlerMovementTests
{
    private readonly Mock<ICharacterRepository> _characterRepository = new();
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
            dispatcher => dispatcher.Dispatch(It.Is<CharacterMovedEvent>(e =>
                e.CharacterId == character.Id &&
                e.Location == character.CurrentLocation)),
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
        _eventDispatcher.Verify(dispatcher => dispatcher.Dispatch(It.IsAny<CharacterMovedEvent>()), Times.Never);
        character.CurrentLocation.Position.Should().Be(Vector3.Zero);
    }
}
