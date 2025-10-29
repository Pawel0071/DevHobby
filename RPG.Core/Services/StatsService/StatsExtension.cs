using RPG.Domain.Enums;

namespace RPG.Core.Services.StatsService;

public static class StatsExtension
{
    public static void CreateEmptyStats(this IDictionary<StatsProperty, int> target)
    {
        foreach (var stat in Enum.GetValues(typeof(StatsProperty)).Cast<StatsProperty>())
        {
            if (!target.ContainsKey(stat))
            {
                target[stat] = 0;
            }
        }
    }

    public static void ToCompleteStats(this IDictionary<StatsProperty, int> target)
    {
        target.CreateEmptyStats(); 
    }
    
    public static void CopyStatsFrom(this IDictionary<StatsProperty, int> target, IDictionary<StatsProperty, int> source)
    {
        if (source == null) return;

        foreach (var kvp in source)
        {
            if (target.ContainsKey(kvp.Key))
                target[kvp.Key] = kvp.Value;
        }
    }
    
    public static void AddStats(
        this IDictionary<StatsProperty, int> target,
        IDictionary<StatsProperty, int> source)
    {
        var result = new Dictionary<StatsProperty, int>();

        foreach (var stat in Enum.GetValues<StatsProperty>())
        {
            var valueA = target.TryGetValue(stat, out var va) ? va : 0;
            var valueB = source.TryGetValue(stat, out var vb) ? vb : 0;
            target[stat] = valueA + valueB;
        }
    }
    
    public static void SubtractStats(
        this IDictionary<StatsProperty, int> target,
        IDictionary<StatsProperty, int> source, bool positiveOnly = false)
    {
        var result = new Dictionary<StatsProperty, int>();

        foreach (var stat in Enum.GetValues<StatsProperty>())
        {
            var valueA = target.TryGetValue(stat, out var va) ? va : 0;
            var valueB = source.TryGetValue(stat, out var vb) ? vb : 0;
            target[stat] = (positiveOnly && valueA < valueB) ? 0 : valueA - valueB;
        }
    }
}