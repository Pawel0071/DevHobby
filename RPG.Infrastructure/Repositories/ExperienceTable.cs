using Newtonsoft.Json;

namespace RPG.Infrastructure.Repositories;

public static class ExperienceData
{
    private static Dictionary<int, int> Levels { get; set; } = new();

    public static bool IsMaxLevel(int level) => !Levels.ContainsKey(level + 1);
    
    public static int GetRequiredExperience(int level)
    {
        if (!Levels.TryGetValue(level, out var xp))
            throw new ArgumentOutOfRangeException(nameof(level), $"Brak danych dla poziomu {level}.");
        return xp;
    }
}

public static class ExperienceTable
{
    public static Dictionary<int, int>? LoadFromJson(string json)
    {
        return JsonConvert.DeserializeObject<Dictionary<int, int>>(json);
    }
}