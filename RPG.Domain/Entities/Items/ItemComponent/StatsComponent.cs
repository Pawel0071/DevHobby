using RPG.Domain.Containers;
using RPG.Domain.Interfaces;

namespace RPG.Domain.Entities.Items.ItemComponent;

public class StatsComponent : IItemComponent
{
    public IStatsContainer Stats { get; init; } = new StatsContainer();
}
