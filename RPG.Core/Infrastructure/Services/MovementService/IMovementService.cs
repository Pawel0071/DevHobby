using System.Numerics;
using RPG.Core.Domain.Entities.Enums;

namespace RPG.Core.MovementService;

public interface IMovementService
{
    event Action<IMovable, Vector3> OnMoved;
    void Move(IMovable entity, MoveType type, int angle);
}