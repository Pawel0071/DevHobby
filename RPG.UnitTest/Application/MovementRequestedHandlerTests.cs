using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RPG.Abstractions.Interfaces;
using RPG.Application.Events;
using RPG.Application.Events.Handlers;
using RPG.Application.Interfaces;
using RPG.Core.Common;
using RPG.Core.Interfaces;
using RPG.Domain.Enums;
using RPG.Domain.Models;
using RPG.Domain.Models.Interaction;
using RPG.Infrastructure.Interfaces;
using Xunit;

namespace RPG.UnitTest.Application;

public class MovementRequestedHandlerTests
{
    [Fact]
    public async Task MovementStartRequested_Should_Update_Character_And_Broadcast_State()
    {
        var characterId = Guid.NewGuid();
        var character = new Character(characterId, CharacterClass.Warrior, null, null)
        {
            Id = characterId,
            Name = "Test"
        };
        character.ModifiedStats[StatsProperty.MoveSpeed] = 5;

        var repo = new Mock<IModelRepository>();
        repo.Setup(r => r.GetByIdAsync<Character>(characterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(character);

        var movement = new Mock<IMovementService>();
        movement.Setup(m => m.Move(character, It.IsAny<System.Numerics.Vector3>(), 0.1f, null, true))
            .Returns(ServiceResult<Location>.Ok(character.CurrentLocation));

        var dispatcher = new Mock<IGameEventDispatcher>();
        var broadcaster = new Mock<ICharacterStateBroadcaster>();
        var logger = new Mock<ILogger<MovementRequestedHandler>>();

        var handler = new MovementRequestedHandler(repo.Object, movement.Object, dispatcher.Object, broadcaster.Object, logger.Object);
        var meta = new EventMetadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);
        var evt = new MovementStartRequestedEvent(meta, characterId, 1, true);

        await handler.HandleAsync(evt, CancellationToken.None);

        repo.Verify(r => r.UpsertAsync(character, It.IsAny<CancellationToken>()), Times.Once);
        broadcaster.Verify(b => b.BroadcastAsync(It.Is<CharacterStateUpdate>(u => u.CharacterId == characterId && u.IsMoving == true), It.IsAny<CancellationToken>()), Times.Once);
    }
}
