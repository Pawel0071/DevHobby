using RPG.Core.Domain.Entities.Enums;

namespace RPG.Core.Domain.Interfaces;

public interface IStatsContainer
{
    int this[StatsProperty property] { get; set; }
    IDictionary<StatsProperty, int> Stats { get; }
}