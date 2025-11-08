using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RPG.Domain.Common;
using RPG.Domain.Interfaces;

namespace RPG.Core.Services.LevelService;

/// <summary>
/// Provides a simple experience table loaded from the predefined level definitions.
/// </summary>
public sealed class DefaultExperienceProvider : IExperienceProvider
{
    private readonly ILogger<DefaultExperienceProvider> _logger;

    public DefaultExperienceProvider(ILogger<DefaultExperienceProvider> logger)
    {
        _logger = logger;
        ExperienceTable = LevelDefinition.Predefined
            .ToDictionary(def => def.Level, def => (int)def.ExperienceToNextLevel);

        if (ExperienceTable.Count == 0)
        {
            _logger.LogWarning("Experience table is empty; leveling will not progress as expected.");
        }
    }

    public Dictionary<int, int> ExperienceTable { get; }

    public int GetRequiredExperience(int level)
    {
        if (ExperienceTable.TryGetValue(level, out var required))
        {
            return required;
        }

        var maxDefinedLevel = ExperienceTable.Keys.DefaultIfEmpty(1).Max();
        if (level <= maxDefinedLevel)
        {
            _logger.LogWarning("Requested experience for level {Level} but it is not defined; returning default 0.", level);
            return 0;
        }

        // Basic extrapolation: reuse last defined requirement to keep service functional.
        var lastValue = ExperienceTable[maxDefinedLevel];
        _logger.LogInformation("Level {Level} is beyond predefined table; using last known requirement {Requirement}.", level, lastValue);
        return lastValue;
    }

    public bool IsMaxLevel(int level)
    {
        var maxDefinedLevel = ExperienceTable.Keys.DefaultIfEmpty(1).Max();
        return level >= maxDefinedLevel;
    }
}
