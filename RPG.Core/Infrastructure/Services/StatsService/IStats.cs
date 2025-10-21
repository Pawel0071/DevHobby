using RPG.Core.Domain.Entities.Enums;

namespace RPG.Core.StatsService;

public interface IStats
{
    Dictionary<StatsProperty, int> BaseStats;
    Dictionary<StatsProperty, int> ModifiedStats;
    int GetStat(StatsProperty property);
    void SetStat(StatsProperty property, int value);
    void ModifyStat(StatsProperty property, int delta);
    Dictionary<StatsProperty, int> GetAllStats();
}