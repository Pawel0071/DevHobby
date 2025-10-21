using System.Numerics;
using RPG.Core.Domain.Entities.Enums;

namespace RPG.Core.MovementService;

public interface IMovable
{
    Vector3 Position { get; set; }
    bool CanMove { get; set; }
    float GetMovementSpeed();
    void Move(MoveType moveType, int angle);
}