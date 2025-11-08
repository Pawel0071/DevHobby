using RPG.Domain.Enums;

namespace RPG.Domain.Common.Interfaces;

public interface IStats
{
    int CurrentHealth { get; set; }
    int MaxHealth { get; set; }
    int CurrentResource { get; set; }
    int MaxResource { get; set; }

    IDictionary<StatsProperty, int> BaseStats { get; }
    IDictionary<StatsProperty, int> ModifiedStats { get; }
}
