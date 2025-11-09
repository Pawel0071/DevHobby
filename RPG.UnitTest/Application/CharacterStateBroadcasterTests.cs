using System;
using System.Numerics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RPG.Application.Broadcasters;
using RPG.Application.Events;
using RPG.Application.Events;
using RPG.Domain.Entities;
using RPG.Domain.Models;
using Xunit;

namespace RPG.UnitTest.GameServer;

public class CharacterStateBroadcasterTests
{
    private readonly CharacterStateBroadcaster _broadcaster;

    public CharacterStateBroadcasterTests()
    {
        var logger = new Mock<ILogger<CharacterStateBroadcaster>>();
        _broadcaster = new CharacterStateBroadcaster(logger.Object);
    }

    [Fact]
    public async Task BroadcastAsync_ShouldPersistInitialSnapshot()
    {
        var characterId = Guid.NewGuid();
        var location = Location.Create(new Vector3(1, 0, 2), Guid.NewGuid(), "test-map", "spawn");
        location.Rotation = 90f;

        var update = new CharacterStateUpdate(characterId, location, IsMoving: true, IsRotating: false, Rotation: 90f);

        await _broadcaster.BroadcastAsync(update);

        var snapshot = _broadcaster.GetSnapshots().Should().ContainSingle().Subject;
        snapshot.CharacterId.Should().Be(characterId);
        snapshot.Location.Position.Should().Be(location.Position);
        snapshot.Location.MapId.Should().Be(location.MapId);
        snapshot.Location.ZoneName.Should().Be(location.ZoneName);
        snapshot.IsMoving.Should().BeTrue();
        snapshot.IsRotating.Should().BeFalse();
        snapshot.Rotation.Should().Be(90f);
    }

    [Fact]
    public async Task BroadcastAsync_ShouldMergeWithExistingState()
    {
        var characterId = Guid.NewGuid();
        var initialLocation = Location.Create(Vector3.Zero, Guid.NewGuid());
        var initialUpdate = new CharacterStateUpdate(characterId, initialLocation, IsMoving: true, Rotation: 0f);
        await _broadcaster.BroadcastAsync(initialUpdate);

        var nextLocation = Location.Create(new Vector3(3, 0, 4), initialLocation.WorldId ?? Guid.NewGuid());
        nextLocation.Rotation = 135f;
        var incrementalUpdate = new CharacterStateUpdate(characterId, nextLocation, IsMoving: null, IsRotating: true, Rotation: 135f);
        await _broadcaster.BroadcastAsync(incrementalUpdate);

        var snapshot = _broadcaster.GetSnapshots().Should().ContainSingle().Subject;
        snapshot.Location.Position.Should().Be(nextLocation.Position);
        snapshot.IsMoving.Should().BeTrue();
        snapshot.IsRotating.Should().BeTrue();
        snapshot.Rotation.Should().Be(135f);
    }
}
