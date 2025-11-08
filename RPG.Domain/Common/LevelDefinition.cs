using RPG.Domain.Common.Interfaces;

namespace RPG.Domain.Common;

/// <summary>
///     Defines level progression data - experience requirements, unlocked skills, and bonuses.
/// </summary>
public sealed class LevelDefinition : IDictionaryEntry<LevelDefinition>
{
    public int Level { get; init; }

    /// <summary>
    ///     Total experience required to reach this level (cumulative from level 1)
    /// </summary>
    public long TotalExperienceRequired { get; init; }

    /// <summary>
    ///     Experience needed to advance from this level to next
    /// </summary>
    public long ExperienceToNextLevel { get; init; }

    /// <summary>
    ///     Skill IDs that become available at this level
    /// </summary>
    public IList<Guid> UnlockedSkills { get; init; } = new List<Guid>();

    /// <summary>
    ///     Number of skill points awarded at this level
    /// </summary>
    public int SkillPointsAwarded { get; init; }

    /// <summary>
    ///     Number of attribute points awarded at this level
    /// </summary>
    public int AttributePointsAwarded { get; init; }

    /// <summary>
    ///     Base health increase per level
    /// </summary>
    public int HealthIncrease { get; init; }

    /// <summary>
    ///     Base resource (mana/energy) increase per level
    /// </summary>
    public int ResourceIncrease { get; init; }

    /// <summary>
    ///     Additional bonuses or unlocks (quest access, areas, features)
    /// </summary>
    public IDictionary<string, object> LevelBonuses { get; init; } = new Dictionary<string, object>();

    public required string Code { get; init; } // np. "1", "2", "3"... lub "Level_1"

    /// <summary>
    ///     Predefined level progression (can be loaded from config/database)
    /// </summary>
    public static IEnumerable<LevelDefinition> Predefined => new[]
    {
        new LevelDefinition
        {
            Code = "1",
            Level = 1,
            TotalExperienceRequired = 0,
            ExperienceToNextLevel = 100,
            SkillPointsAwarded = 0,
            AttributePointsAwarded = 0,
            HealthIncrease = 100,
            ResourceIncrease = 50
        },
        new LevelDefinition
        {
            Code = "2",
            Level = 2,
            TotalExperienceRequired = 100,
            ExperienceToNextLevel = 150,
            SkillPointsAwarded = 1,
            AttributePointsAwarded = 5,
            HealthIncrease = 20,
            ResourceIncrease = 10
        },
        new LevelDefinition
        {
            Code = "3",
            Level = 3,
            TotalExperienceRequired = 250,
            ExperienceToNextLevel = 200,
            SkillPointsAwarded = 1,
            AttributePointsAwarded = 5,
            HealthIncrease = 20,
            ResourceIncrease = 10
        },
        new LevelDefinition
        {
            Code = "4",
            Level = 4,
            TotalExperienceRequired = 450,
            ExperienceToNextLevel = 250,
            SkillPointsAwarded = 1,
            AttributePointsAwarded = 5,
            HealthIncrease = 20,
            ResourceIncrease = 10
        },
        new LevelDefinition
        {
            Code = "5",
            Level = 5,
            TotalExperienceRequired = 700,
            ExperienceToNextLevel = 300,
            SkillPointsAwarded = 2,
            AttributePointsAwarded = 5,
            HealthIncrease = 30,
            ResourceIncrease = 15
        }
        // ... można dodać więcej poziomów
    };

    /// <summary>
    ///     Get level definition by level number
    /// </summary>
    public static LevelDefinition? GetByLevel(int level)
    {
        return Predefined.FirstOrDefault(l => l.Level == level);
    }

    /// <summary>
    ///     Calculate which level should character be at based on total experience
    /// </summary>
    public static int CalculateLevelFromExperience(long totalExperience)
    {
        var levels = Predefined.OrderByDescending(l => l.Level);
        foreach (var levelDef in levels)
            if (totalExperience >= levelDef.TotalExperienceRequired)
                return levelDef.Level;
        return 1;
    }

    /// <summary>
    ///     Get experience required to reach next level from current experience
    /// </summary>
    public static long GetExperienceToNextLevel(long currentExperience)
    {
        var currentLevel = CalculateLevelFromExperience(currentExperience);
        var nextLevelDef = GetByLevel(currentLevel + 1);

        if (nextLevelDef == null)
            return 0; // Max level reached

        return nextLevelDef.TotalExperienceRequired - currentExperience;
    }
}
