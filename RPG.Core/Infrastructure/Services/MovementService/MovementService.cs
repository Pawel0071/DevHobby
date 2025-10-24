using System.Numerics;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Infrastructure.Services.MovementService;

namespace RPG.Core.MovementService;

public class MovementService : IMovementService
{
    public event Action<IMovable, Vector3> OnMoved = null!;
    public event Action<IMovable>? OnMoveStarted;
    public event Action<IMovable>? OnMoveStopped;

    public void Move(IMovable entity, MoveType type, int angle)
    {
        OnMoveStarted?.Invoke(entity);
        var baseSpeed = entity.GetMovementSpeed();
        var speed = type switch
        {
            MoveType.Walk => baseSpeed / 2,
            MoveType.Ride => baseSpeed * 2,
            _ => baseSpeed
        };
        var radians = angle * Math.PI / 180;
        var dx = speed * Math.Cos(radians);
        var dy = speed * Math.Sin(radians);
        var movementVector = new Vector3((float)dx, (float)dy,(float)0);
        entity.Position += movementVector;
        OnMoved?.Invoke(entity, movementVector);
        OnMoveStopped?.Invoke(entity);
    }
}