using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Interfaces;
using RPG.Core.Infrastructure.Services.StatsService;
using RPG.Core.Interfaces;

namespace RPG.Core.Infrastructure.Services.LevelSerivice;

public class LevelingService : ILevelingService
{
    private readonly IStatsService _statsService;
    private readonly ISkillService _skillService;

    public LevelingService(IStatsService statsService,
        ISkillService skillService)
    {
        _statsService = statsService;
        _skillService = skillService;
    }
    
    public LevelingResult LevelUp(Character character, int amount = 0)
    {
        character.Level++;
        character.Experience = amount;
        _statsService.RegenerateStatsAfterLevelUp(character); 
        _skillService.AddSkillsAfterLevelUp(character); 
        return LevelingResult.Ok();
    }

    public LevelingResult GrantExperience(Character character, int amount)
    {
        character.Experience += amount;
        character.ExperienceToNextLevel -= amount;
        
        if (character.ExperienceToNextLevel <= 0)
        {
            LevelUp(character, -character.ExperienceToNextLevel);
        }
        return LevelingResult.Ok();
    }
}