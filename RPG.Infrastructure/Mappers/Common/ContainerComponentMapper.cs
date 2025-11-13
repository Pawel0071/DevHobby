using System.Linq;
using RPG.Domain.Common;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.MapObjects.MapObjectComponents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Models;
using DomainInventorySlot = RPG.Domain.Common.InventorySlot;

namespace RPG.Infrastructure.Mappers.Common;

internal static class ContainerComponentMapper
{
    public static ContainerComponentDto ToDto(
        ContainerComponent component,
        IModelMapper<Item, ItemDocument> itemMapper)
    {
        return new ContainerComponentDto
        {
            Capacity = component.GetContainer().Capacity,
            Items = component.Items
                .Select(slot => new InventorySlotDto
                {
                    Item = slot.Item is null ? null : itemMapper.ToPersistence(slot.Item),
                    Quantity = slot.Quantity
                })
                .ToList()
        };
    }

    public static ContainerComponent? FromDto(
        ContainerComponentDto? dto,
        IModelMapper<Item, ItemDocument> itemMapper)
    {
        if (dto is null)
        {
            return null;
        }

        var component = new ContainerComponent(dto.Capacity);
        var container = component.GetContainer();

        if (dto.Items is not null && dto.Items.Count > 0)
        {
            container.Inventory = dto.Items
                .Select(slot => new DomainInventorySlot
                {
                    Item = slot.Item is null ? null : itemMapper.ToDomain(slot.Item),
                    Quantity = slot.Quantity
                })
                .ToList();
        }

        return component;
    }
}
