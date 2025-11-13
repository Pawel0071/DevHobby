using RPG.Core.Common;
using RPG.Domain.Models;

namespace RPG.Core.Interfaces;

public interface ILevelingService
{
    ServiceResult<bool> LevelUp(Character character, long amount);
    ServiceResult<bool> GrantExperience(Character character, long amount);
}
