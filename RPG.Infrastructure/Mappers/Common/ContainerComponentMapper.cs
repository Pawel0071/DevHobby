using System.Linq;
using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.MapObjects.MapObjectComponents;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using DomainInventorySlot = RPG.Domain.Common.InventorySlot;

namespace RPG.Infrastructure.Mappers.Common;

internal static class ContainerComponentMapper
{
    public static ContainerComponentDto ToDto(
        ContainerComponent component,
        IDocumentMapper<Item, ItemDocument> itemMapper)
    {
        return new ContainerComponentDto
        {
            Capacity = component.GetContainer().Capacity,
            Items = component.Items
                .Select(slot => new InventorySlotDto
                {
                    Item = slot.Item is null ? null : itemMapper.ToDocument(slot.Item),
                    Quantity = slot.Quantity
                })
                .ToList()
        };
    }

    public static ContainerComponent? FromDto(
        ContainerComponentDto? dto,
        IDocumentMapper<Item, ItemDocument> itemMapper)
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
