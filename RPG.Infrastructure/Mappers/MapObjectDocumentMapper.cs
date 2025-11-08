using System.Text.Json;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.MapObjects.MapObjectComponents;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers.Common;

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between MapObject domain entity and MapObjectDocument
///     Components are serialized to JSON for flexible storage
/// </summary>
public class MapObjectDocumentMapper : IDocumentMapper<MapObject, MapObjectDocument>
{
    private readonly ILogger<MapObjectDocumentMapper> _logger;
    private readonly LocationMapper _locationMapper;
    private readonly IDocumentMapper<Item, ItemDocument> _itemMapper;

    public MapObjectDocumentMapper(
        ILogger<MapObjectDocumentMapper> logger,
        LocationMapper locationMapper,
        IDocumentMapper<Item, ItemDocument> itemMapper)
    {
        _logger = logger;
        _locationMapper = locationMapper;
        _itemMapper = itemMapper;
    }

    public MapObjectDocument ToDocument(MapObject entity)
    {
        _logger.Debug($"Converting MapObject to MapObjectDocument. Id={entity.Id}, Name={entity.Name}");
        return new MapObjectDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            Location = _locationMapper.ToDocument(entity.Location),
            RotationYaw = entity.RotationYaw,
            WorldId = entity.WorldId,
            ZoneId = entity.ZoneId,
            IsActive = entity.IsActive,
            Tags = entity.Tags.ToList(),
            Components = entity.Components
                .Select(component => SerializeComponent(component))
                .ToList()
        };
    }

    public MapObject ToDomain(MapObjectDocument document)
    {
        _logger.Debug($"Converting MapObjectDocument to MapObject. Id={document.Id}, Name={document.Name}");
        var location = _locationMapper.ToEntity(document.Location);
        var mapObject = MapObject.Create(
            document.Name,
            location,
            document.WorldId,
            document.ZoneId);

        // Preserve ID from document using reflection
        typeof(MapObject).GetProperty("Id")!.SetValue(mapObject, document.Id);

        mapObject.DisplayName = document.DisplayName;
        mapObject.Description = document.Description;
        mapObject.RotationYaw = document.RotationYaw;
        mapObject.IsActive = document.IsActive;
        mapObject.Tags = document.Tags.ToHashSet();

        // Deserialize components
        foreach (var componentData in document.Components)
        {
            var component = DeserializeComponent(componentData);
            if (component != null)
            {
                mapObject.Components.Add(component);
            }
        }

        return mapObject;
    }

    public MapObject ToEntity(MapObjectDocument document) => ToDomain(document);
    
    private ComponentData SerializeComponent(IMapObjectComponent component)
    {
        var type = component.GetType();
        var serialized = type == typeof(ContainerComponent)
            ? JsonSerializer.Serialize(ContainerComponentMapper.ToDto((ContainerComponent)component, _itemMapper))
            : JsonSerializer.Serialize(component, type);

        return new ComponentData
        {
            Type = type.Name,
            Data = serialized
        };
    }

    private IMapObjectComponent? DeserializeComponent(ComponentData componentData)
    {
        try
        {
            return componentData.Type switch
            {
                nameof(ContainerComponent) => DeserializeContainerComponent(componentData.Data),
                nameof(LockableComponent) => JsonSerializer.Deserialize<LockableComponent>(componentData.Data),
                nameof(DoorComponent) => JsonSerializer.Deserialize<DoorComponent>(componentData.Data),
                nameof(TriggerComponent) => JsonSerializer.Deserialize<TriggerComponent>(componentData.Data),
                nameof(CraftingStationComponent) =>
                    JsonSerializer.Deserialize<CraftingStationComponent>(componentData.Data),
                nameof(ResourceNodeComponent) => JsonSerializer.Deserialize<ResourceNodeComponent>(componentData.Data),
                nameof(DestructibleComponent) => JsonSerializer.Deserialize<DestructibleComponent>(componentData.Data),
                nameof(PortalComponent) => JsonSerializer.Deserialize<PortalComponent>(componentData.Data),
                nameof(InteractionComponent) => JsonSerializer.Deserialize<InteractionComponent>(componentData.Data),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to deserialize map object component '{componentData.Type}'. Skipping. Error: {ex.Message}");
            return null;
        }
    }

    private ContainerComponentDto ToContainerDto(ContainerComponent component)
    {
        return new ContainerComponentDto
        {
            Capacity = component.GetContainer().Capacity,
            Items = component.Items
                .Select(slot => new InventorySlotDto
                {
                    Item = slot.Item is null ? null : _itemMapper.ToDocument(slot.Item),
                    Quantity = slot.Quantity
                })
                .ToList()
        };
    }

    private ContainerComponent? DeserializeContainerComponent(string data)
    {
        var dto = JsonSerializer.Deserialize<ContainerComponentDto>(data);
        return ContainerComponentMapper.FromDto(dto, _itemMapper);
    }
}
