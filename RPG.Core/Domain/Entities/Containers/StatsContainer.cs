using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Domain.Interfaces;

namespace RPG.Core.Domain.Entities.Containers;

public class StatsContainer : IStatsContainer
{
    public IDictionary<StatsProperty, int> Stats { get; }

    public StatsContainer()
    {
        Stats = Enum.GetValues(typeof(StatsProperty))
            .Cast<StatsProperty>()
            .ToDictionary(stat => stat, stat => 0);
    }
    
    public int this[StatsProperty property]
    {
        get => Stats[property];
        set => Stats[property] = value;
    }
    
}

