using System.Numerics;
using RPG.Core.Common;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Npcs;

namespace RPG.Core.Interfaces;

public interface IMovementService
{
    ServiceResult<Location> Move(Character character, Vector3 direction, float deltaTime, float? speedOverride = null);
    ServiceResult<Location> Move(Npc npc, Vector3 direction, float deltaTime, float? speedOverride = null);
}
