using RPG.Domain.Common;
using RPG.Domain.Containers;

namespace RPG.Domain.Models.MapObjects.MapObjectComponents;

/// <summary>
///     Component for map objects that can contain items (chests, crates, barrels, etc.).
///     Pure data - container logic handled by services.
/// </summary>
public class ContainerComponent : IMapObjectComponent
{
    private readonly InventoryContainer _container;

    public ContainerComponent(int capacity = 20)
    {
        _container = new InventoryContainer(capacity);
    }

    public IList<InventorySlot> Items => _container.Inventory;

    public InventoryContainer GetContainer()
    {
        return _container;
    }
}
