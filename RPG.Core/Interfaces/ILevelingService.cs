using RPG.Core.Common;
using RPG.Domain.Entities;

namespace RPG.Core.Interfaces;

public interface ILevelingService
{
    ServiceResult<bool> LevelUp(Character character, long amount);
    ServiceResult<bool> GrantExperience(Character character, long amount);
}
