using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Interfaces;

namespace RPG.Core.Domain.Entities.Common;

public class Stats : IStats
{
    private readonly Dictionary<StatsProperty, int> _statsDictionary;

    public Stats(Dictionary<StatsProperty, int> allStats)
    {
        _statsDictionary = Enum.GetValues(typeof(StatsProperty))
            .Cast<StatsProperty>()
            .ToDictionary(stat => stat, stat => 0);
    }

 

    public int GetStat(StatsProperty property)
    {
        return _statsDictionary.TryGetValue(property, out var value) ? value : 0;
    }

    public void SetStat(StatsProperty property, int value)
    {
        if (_statsDictionary.ContainsKey(property))
        {
            _statsDictionary[property] = value;
        }
    }

    public void ModifyStat(StatsProperty property, int delta)
    {
        if (_statsDictionary.ContainsKey(property))
        {
            _statsDictionary[property] += delta;
        }
    }
    
    public Dictionary<StatsProperty, int>  GetAllStats()
    {
        return new Dictionary<StatsProperty, int>(_statsDictionary);
    }   
}

