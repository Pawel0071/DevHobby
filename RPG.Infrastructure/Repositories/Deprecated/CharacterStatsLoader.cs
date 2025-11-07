using System.Text.Json;
using RPG.Domain.Enums;

namespace RPG.Infrastructure.Repositories.Deprecated;

public static class CharacterStatsLoader
{
    public static Dictionary<StatsProperty, int> LoadStatsForClass(CharacterClass characterClass, string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var rawData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json);

        if (rawData == null || !rawData.TryGetValue(characterClass.ToString(), out var classStats))
            throw new Exception($"Brak danych dla klasy: {characterClass}");

        var stats = new Dictionary<StatsProperty, int>();

        foreach (var entry in classStats)
        {
            if (Enum.TryParse<StatsProperty>(entry.Key, out var stat))
                stats[stat] = entry.Value;
        }

        return stats;
    }
}