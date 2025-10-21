namespace RPG.Core.StatsService;

public class StatsService :IStatsService
{
    public void InitStats(IStats stats)
    {
        stats.BaseStats.CreateEmptyStats();
        stats.ModifiedStats.CopyStatsFrom(stats.BaseStats);
    }
}  