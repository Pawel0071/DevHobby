using RPG.Core.Domain.Entities;

namespace RPG.Core.Infrastructure.Services.LevelSerivice;

public interface ILevelingService
{
    LevelingResult LevelUp(Character character, int amount);
    LevelingResult GrantExperience(Character character, int amount);

}