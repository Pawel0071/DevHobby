using System.Numerics;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.MovementService;

namespace RPG.Core.Infrastructure.Services.MovementService;

public interface IMovementService
{
    event Action<IMovable, Vector3> OnMoved;
    void Move(IMovable entity, MoveType type, int angle);
}