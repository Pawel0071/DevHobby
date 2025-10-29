using RPG.Domain.Interfaces;

namespace RPG.Domain.Common.Interfaces;

public interface IStats
{
    int CurrentHealth { get; set; }
    int MaxHealth { get; set; }
    int CurrentResource { get; set; }
    int MaxResource { get; set; }
    
    IStatsContainer BaseStats { get; set; }
    IStatsContainer ModifiedStats { get; set; }
}