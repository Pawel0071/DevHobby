using System.Numerics;
using FluentAssertions;
using Moq;
using RPG.Core.Services.MovementService;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Enums;
using RPG.Infrastructure.Interfaces;

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
    }

    [Fact]
    public void MoveNpc_ShouldUpdatePosition()
    {
        var spawn = Location.Create(Vector3.Zero, Guid.NewGuid());
        var npc = Npc.Create("mob.wolf", "Wolf", spawn, Guid.NewGuid());
        npc.ModifiedStats[StatsProperty.MoveSpeed] = 4;

        var result = _movementService.Move(npc, new Vector3(0, 0, 1), 2f);

        result.Success.Should().BeTrue();
        npc.CurrentLocation.Position.Z.Should().BeApproximately(8f, 0.0001f);
        npc.CurrentLocation.Position.X.Should().BeApproximately(0f, 0.0001f);
        npc.CurrentLocation.Rotation.Should().BeApproximately(0f, 0.0001f);
    }
}
