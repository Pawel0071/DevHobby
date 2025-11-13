using System.Numerics;
using FluentAssertions;
using Moq;
using RPG.Core.Services.MovementService;
using RPG.Domain.Common;
using RPG.Domain.Enums;
using RPG.Domain.Models;
using RPG.Domain.Models.Npcs;
using RPG.Infrastructure.Interfaces;
using Xunit;

namespace RPG.UnitTest.Core.MovementServiceTests;

public class MovementServiceTests
{
    private readonly MovementService _movementService;

    public MovementServiceTests()
    {
        var logger = new Mock<ILogger<MovementService>>();
        _movementService = new MovementService(logger.Object);
    }

    [Fact]
    public void Move_ShouldAdvanceCharacterUsingMoveSpeed()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "Runner"
        };

        var worldId = Guid.NewGuid();
        character.SetCurrentLocation(Location.Create(Vector3.Zero, worldId));
        character.ModifiedStats[StatsProperty.MoveSpeed] = 6;

        var result = _movementService.Move(character, new Vector3(1, 0, 0), deltaTime: 1.5f);

        result.Success.Should().BeTrue();
        character.CurrentLocation.Position.X.Should().BeApproximately(9f, 0.0001f);
        character.CurrentLocation.Position.Y.Should().Be(0f);
        character.CurrentLocation.Position.Z.Should().Be(0f);
        character.CurrentLocation.Rotation.Should().BeApproximately(90f, 0.0001f);
        character.IsMoving.Should().BeTrue();
    }

    [Fact]
    public void Move_WithPreserveFacing_ShouldNotChangeRotation()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "StrafeTester"
        };

        var worldId = Guid.NewGuid();
        var location = Location.Create(Vector3.Zero, worldId);
        location.Rotation = 180f;
        character.SetCurrentLocation(location);
        character.ModifiedStats[StatsProperty.MoveSpeed] = 4;

        var result = _movementService.Move(character, new Vector3(1, 0, 0), deltaTime: 0.5f, preserveFacing: true);

        result.Success.Should().BeTrue();
        character.CurrentLocation.Position.X.Should().BeApproximately(2f, 0.0001f);
        character.CurrentLocation.Rotation.Should().BeApproximately(180f, 0.0001f);
    }

    [Fact]
    public void Move_WhenDirectionIsZero_ShouldFail()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Mage)
        {
            Id = Guid.NewGuid(),
            Name = "Static"
        };

        character.ModifiedStats[StatsProperty.MoveSpeed] = 5;

        var result = _movementService.Move(character, Vector3.Zero, 1f);

        result.Success.Should().BeFalse();
        result.Error.Should().Be(ErrorCodeDefinition.MovementInvalidDirection);
        character.IsMoving.Should().BeFalse();
    }

    [Fact]
    public void MoveNpc_ShouldUpdatePosition()
    {
        var spawn = Location.Create(Vector3.Zero, Guid.NewGuid());
        var npc = Npc.Create("mob.wolf", "Wolf", spawn, Guid.NewGuid());
        npc.ModifiedStats[StatsProperty.MoveSpeed] = 4;

    var result = _movementService.Move(npc, new Vector3(0, 1, 0), 2f);

        result.Success.Should().BeTrue();
    npc.CurrentLocation.Position.Y.Should().BeApproximately(8f, 0.0001f);
        npc.CurrentLocation.Position.X.Should().BeApproximately(0f, 0.0001f);
    npc.CurrentLocation.Rotation.Should().BeApproximately(0f, 0.0001f);
        npc.IsMoving.Should().BeTrue();
    }

    [Fact]
    public void Rotate_ShouldUpdateCharacterYaw()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "Spinner"
        };
        character.SetCurrentLocation(Location.Create(Vector3.Zero, Guid.NewGuid()));

    var result = _movementService.Rotate(character, new Vector3(1, 1, 0));

        result.Success.Should().BeTrue();
        result.Result.Should().BeApproximately(45f, 0.0001f);
        character.CurrentLocation.Rotation.Should().BeApproximately(45f, 0.0001f);
        character.IsRotating.Should().BeTrue();
    }

    [Fact]
    public void Rotate_WithInvalidDirection_ShouldFail()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = "Shaky"
        };
        character.SetCurrentLocation(Location.Create(Vector3.Zero, Guid.NewGuid()));

        var result = _movementService.Rotate(character, Vector3.Zero);

        result.Success.Should().BeFalse();
        result.Error.Should().Be(ErrorCodeDefinition.MovementInvalidDirection);
        character.IsRotating.Should().BeFalse();
    }

    [Fact]
    public void Stop_ShouldReturnCurrentLocation()
    {
        var worldId = Guid.NewGuid();
        var character = new Character(Guid.NewGuid(), CharacterClass.Mage)
        {
            Id = Guid.NewGuid(),
            Name = "Breaker"
        };
        var location = Location.Create(new Vector3(3, 0, -2), worldId);
        character.SetCurrentLocation(location);
        character.SetMovementState(true);

        var result = _movementService.Stop(character);

        result.Success.Should().BeTrue();
        result.Result.Should().BeSameAs(location);
        character.IsMoving.Should().BeFalse();
    }

    [Fact]
    public void StopRotation_ShouldReturnCurrentYaw()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Mage)
        {
            Id = Guid.NewGuid(),
            Name = "YawKeeper"
        };
        var location = Location.Create(Vector3.Zero, Guid.NewGuid());
        location.Rotation = 123.4f;
        character.SetCurrentLocation(location);
        character.SetRotationState(true);

        var result = _movementService.StopRotation(character);

        result.Success.Should().BeTrue();
        result.Result.Should().BeApproximately(123.4f, 0.0001f);
        character.IsRotating.Should().BeFalse();
    }
}
