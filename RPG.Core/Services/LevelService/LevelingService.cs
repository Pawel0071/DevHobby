using RPG.Core.Common;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.LevelService;

public class LevelingService : ILevelingService
{
    private readonly IExperienceProvider _experienceProvider;
    private readonly ILogger<LevelingService> _logger;
    private readonly ISkillService _skillService;
    private readonly IStatsService _statsService;

    public LevelingService(
        IStatsService statsService,
        ISkillService skillService,
        IExperienceProvider experienceProvider,
        ILogger<LevelingService> logger)
    {
        _statsService = statsService;
        _skillService = skillService;
        _experienceProvider = experienceProvider;
        _logger = logger;
    }

    public ServiceResult<bool> LevelUp(Character character, long amount = 0)
    {
        _logger.Debug($"Attempting to level up character '{character.Id}' at level {character.Level}.");

        if (_experienceProvider.IsMaxLevel(character.Level))
        {
            _logger.Warn($"Character '{character.Id}' is already at max level ({character.Level}).");
            return ErrorCodeDefinition.AlreadyMaxLevel.ToFail<bool>($"Poziom maksymalny: {character.Level}");
        }

        character.Level++;
        character.Experience = amount;

        _logger.Info($"Character '{character.Id}' leveled up to {character.Level}. Experience reset to {amount}.");

        _statsService.RegenerateStatsAfterLevelUp(character);
        _skillService.AddSkillsAfterLevelUp(character);

        character.ExperienceToNextLevel = _experienceProvider.GetRequiredExperience(character.Level);

        _logger.Debug($"New experience requirement for level {character.Level}: {character.ExperienceToNextLevel}.");

        return true.ToResult();
    }

    public ServiceResult<bool> GrantExperience(Character character, long amount)
    {
        _logger.Debug($"Granting {amount} XP to character '{character.Id}' at level {character.Level}.");

        if (_experienceProvider.IsMaxLevel(character.Level))
        {
            _logger.Warn($"Character '{character.Id}' is already at max level ({character.Level}). XP grant ignored.");
            return ErrorCodeDefinition.AlreadyMaxLevel.ToFail<bool>($"Poziom maksymalny: {character.Level}");
        }

        character.Experience += amount;
        character.ExperienceToNextLevel -= amount;

        _logger.Info(
            $"Character '{character.Id}' now has {character.Experience} XP. Remaining to next level: {character.ExperienceToNextLevel}.");

        if (character.ExperienceToNextLevel <= 0)
        {
            _logger.Debug($"Character '{character.Id}' has enough XP to level up. Triggering LevelUp.");
            return LevelUp(character, -character.ExperienceToNextLevel);
        }

        return true.ToResult();
    }
}
