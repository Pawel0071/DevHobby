using RPG.Domain.Enums;
using RPG.Domain.Interfaces;

namespace RPG.Domain.Containers;

public class StatsContainer : IStatsContainer
{
    public IDictionary<StatsProperty, int> Stats { get; set; }

    public StatsContainer()
    {
        Stats = Enum.GetValues(typeof(StatsProperty))
            .Cast<StatsProperty>()
            .ToDictionary(stat => stat, stat => 0);
    }

    public StatsContainer(IDictionary<StatsProperty, int> stats)
    {
        Stats = stats;
    }
    
    public int this[StatsProperty property]
    {
        get => Stats[property];
        set => Stats[property] = value;
    }
    
}

