using RPG.Domain.Enums;

namespace RPG.Domain.Interfaces;

public interface IStatsContainer
{
    int this[StatsProperty property] { get; set; }
    IDictionary<StatsProperty, int> Stats { get; set; }
}
