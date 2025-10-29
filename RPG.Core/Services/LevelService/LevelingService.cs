using RPG.Core.Interfaces;
using RPG.Domain.Entities;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Logger;

namespace RPG.Core.Services.LevelService;

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
        _logger.Debug($"Attempting to level up character '{character.Id}' at level {character.Level}.");

        if (_experienceProvider.IsMaxLevel(character.Level))
        {
            _logger.Warn($"Character '{character.Id}' is already at max level ({character.Level}).");
            return LevelingResult.Fail(LevelingError.AlreadyMaxLevel, $"{character.Level}");
        }

        character.Level++;
        character.Experience = amount;

        _logger.Info($"Character '{character.Id}' leveled up to {character.Level}. Experience reset to {amount}.");

        _statsService.RegenerateStatsAfterLevelUp(character);
        _skillService.AddSkillsAfterLevelUp(character);

        character.ExperienceToNextLevel = _experienceProvider.GetRequiredExperience(character.Level);

        _logger.Debug($"New experience requirement for level {character.Level}: {character.ExperienceToNextLevel}.");

        return LevelingResult.Ok();
    }

    public LevelingResult GrantExperience(Character character, int amount)
    {
        _logger.Debug($"Granting {amount} XP to character '{character.Id}' at level {character.Level}.");

        if (_experienceProvider.IsMaxLevel(character.Level))
        {
            _logger.Warn($"Character '{character.Id}' is already at max level ({character.Level}). XP grant ignored.");
            return LevelingResult.Fail(LevelingError.AlreadyMaxLevel, $"{character.Level}");
        }

        character.Experience += amount;
        character.ExperienceToNextLevel -= amount;

        _logger.Info($"Character '{character.Id}' now has {character.Experience} XP. Remaining to next level: {character.ExperienceToNextLevel}.");

        if (character.ExperienceToNextLevel <= 0)
        {
            _logger.Debug($"Character '{character.Id}' has enough XP to level up. Triggering LevelUp.");
            return LevelUp(character, -character.ExperienceToNextLevel);
        }

        return LevelingResult.Ok();
    }
}