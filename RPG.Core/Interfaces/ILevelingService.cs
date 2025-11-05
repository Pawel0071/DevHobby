using RPG.Core.Common;
using RPG.Core.Services.LevelService;
using RPG.Domain.Entities;

namespace RPG.Core.Interfaces;

public interface ILevelingService
{
    ServiceResult<bool> LevelUp(Character character, int amount);
    ServiceResult<bool> GrantExperience(Character character, int amount);

}