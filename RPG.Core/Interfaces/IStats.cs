using RPG.Core.Domain.Entities.Enums;

namespace RPG.Core.Interfaces;

public interface IStats
{
    int GetStat(StatsProperty property);
    void SetStat(StatsProperty property, int value);
    void ModifyStat(StatsProperty property, int delta);
    Dictionary<StatsProperty, int> GetAllStats();
}