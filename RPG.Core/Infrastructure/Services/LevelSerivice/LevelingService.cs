using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Interfaces;
using RPG.Core.Infrastructure.Interfaces;
using RPG.Core.Infrastructure.Services.Logger;
using RPG.Core.Infrastructure.Services.StatsService;
using RPG.Core.Interfaces;

namespace RPG.Core.Infrastructure.Services.LevelSerivice;

public class LevelingService : ILevelingService
{
    private readonly IStatsService _statsService;
    private readonly ISkillService _skillService;
    private readonly IExperienceProvider _experienceProvider;
    private readonly ILogger<LevelingService> _logger;

    public LevelingService(IStatsService statsService,
        ISkillService skillService,
        IExperienceProvider experienceProvider,
        ILogger<LevelingService> logger)
    {
        _statsService = statsService;
        _skillService = skillService;
        _experienceProvider = experienceProvider;
        _logger = logger;
    }
    
    public LevelingResult LevelUp(Character character, int amount = 0)
    {
        if (_experienceProvider.IsMaxLevel(character.Level))
        {
            LevelingResult.Fail(LevelingError.AlreadyMaxLevel, $"{character.Level}");
        }
        
        character.Level++;
        character.Experience = amount;
        _statsService.RegenerateStatsAfterLevelUp(character); 
        _skillService.AddSkillsAfterLevelUp(character);
        character.ExperienceToNextLevel = _experienceProvider.GetRequiredExperience(character.Level);
        return LevelingResult.Ok();
    }

    public LevelingResult GrantExperience(Character character, int amount)
    {
        if (_experienceProvider.IsMaxLevel(character.Level))
        {
            LevelingResult.Fail(LevelingError.AlreadyMaxLevel, $"{character.Level}");
        }
        
        character.Experience += amount;
        character.ExperienceToNextLevel -= amount;
        
        if (character.ExperienceToNextLevel <= 0)
        {
            LevelUp(character, -character.ExperienceToNextLevel);
        }
        return LevelingResult.Ok();
    }
}