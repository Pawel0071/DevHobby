using System.Numerics;
using RPG.Core.Common;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Npcs;

namespace RPG.Core.Interfaces;

public interface IMovementService
{
    ServiceResult<Location> Move(Character character, Vector3 direction, float deltaTime, float? speedOverride = null, bool preserveFacing = false);
    ServiceResult<Location> Move(Npc npc, Vector3 direction, float deltaTime, float? speedOverride = null, bool preserveFacing = false);
    ServiceResult<Location> Stop(Character character);
    ServiceResult<Location> Stop(Npc npc);
    ServiceResult<float> Rotate(Character character, Vector3 direction);
    ServiceResult<float> Rotate(Npc npc, Vector3 direction);
    ServiceResult<float> StopRotation(Character character);
    ServiceResult<float> StopRotation(Npc npc);
}
