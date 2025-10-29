using RPG.Core.Services.LevelService;
using RPG.Domain.Entities;

namespace RPG.Core.Interfaces;

public interface ILevelingService
{
    LevelingResult LevelUp(Character character, int amount);
    LevelingResult GrantExperience(Character character, int amount);

}