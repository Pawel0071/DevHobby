using System.Numerics;
using RPG.Core.Common;
using RPG.Domain.Interfaces;
using RPG.Domain.Models;
using RPG.Domain.Models.Npcs;

namespace RPG.Core.Interfaces;

public interface IMovementService
{
    ServiceResult<Location> Move(IMovable character, Vector3 direction, float deltaTime, float? speedOverride = null, bool preserveFacing = false);
    ServiceResult<Location> Stop(IMovable character);
    ServiceResult<float> Rotate(IMovable character, Vector3 direction);
    ServiceResult<float> StopRotation(IMovable character);
}
