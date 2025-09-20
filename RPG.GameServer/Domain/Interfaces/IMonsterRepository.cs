using System.Collections;
using RPG.GameServer.Domain.Entities;
using RPG.GameServer.Domain.Entities.Common;

namespace RPG.GameServer.Interfaces;

public interface IMonsterRepository
{
    IEnumerable<MonsterCharacter<IAiBehavior>> GetMonstersNear(Location evtNewPosition, float radius);
}