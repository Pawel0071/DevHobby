using RPG.Core.Domain.Entities.Enums;

namespace RPG.Core.Infrastructure.Services.StatsService;

public static class StatsInitializer
{
    public static Dictionary<StatsProperty, int> CreateEmptyStats(this Dictionary<StatsProperty, int> _)
    {
        return Enum.GetValues(typeof(StatsProperty))
            .Cast<StatsProperty>()
            .ToDictionary(stat => stat, stat => 0);
    }

    public static Dictionary<StatsProperty, int> ToCompleteStats(this Dictionary<StatsProperty, int>? source)
    {
        var complete = Enum.GetValues(typeof(StatsProperty))
            .Cast<StatsProperty>()
            .ToDictionary(stat => stat, stat => 0);

        if (source == null) return complete;

        foreach (var kvp in source)
        {
            complete[kvp.Key] = kvp.Value;
        }

        return complete;
    }
    
    public static void CopyStatsFrom(this Dictionary<StatsProperty, int> target, Dictionary<StatsProperty, int>? source)
    {
        if (source == null) return;

        foreach (var kvp in source)
        {
            if (target.ContainsKey(kvp.Key))
                target[kvp.Key] = kvp.Value;
        }
    }
}