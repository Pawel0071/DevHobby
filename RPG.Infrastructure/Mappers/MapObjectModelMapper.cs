using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers.Common;
using RPG.Domain.Enums; // TagTarget
using RPG.Abstractions; // TagComponentMap (global tag->component map)
using RPG.Domain.Common;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.MapObjects;
using RPG.Domain.Models.MapObjects.MapObjectComponents;
using RPG.Infrastructure.Models;

// TagDefinition access

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between MapObject domain entity and MapObjectDocument
///     Components are serialized to JSON for flexible storage
/// </summary>
public class MapObjectModelMapper : IModelMapper<MapObject, MapObjectDocument>
{
    private readonly ILogger<MapObjectModelMapper> _logger;
    private readonly LocationMapper _locationMapper;
    private readonly IModelMapper<Item, ItemDocument> _itemMapper;

    public MapObjectModelMapper(
        ILogger<MapObjectModelMapper> logger,
        LocationMapper locationMapper,
        IModelMapper<Item, ItemDocument> itemMapper)
    {
        _logger = logger;
        _locationMapper = locationMapper;
        _itemMapper = itemMapper;
    }

    public MapObjectDocument ToPersistence(MapObject entity)
    {
        _logger.Debug($"Converting MapObject to MapObjectDocument. Id={entity.Id}, Name={entity.Name}");
        // Merge derived tags
        var derived = ResolveComponentTags(entity.Components.Select(c => c.GetType()), TagTarget.MapObject);
        foreach (var t in derived) entity.Tags.Add(t);
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
                .ToList(),
            State = entity.State?.Count > 0
                ? entity.State.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            LastUpdated = entity.LastUpdated
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
        mapObject.State = document.State?.Count > 0
            ? document.State.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (document.LastUpdated != default)
        {
            mapObject.LastUpdated = document.LastUpdated;
        }

        // Deserialize components
        foreach (var componentData in document.Components)
        {
            var component = DeserializeComponent(componentData);
            if (component != null)
            {
                mapObject.Components.Add(component);
            }
        }

        // Auto-add missing components based on tags
        var requiredTypes = TagComponentMap.GetRequiredComponentTypes(mapObject.Tags, TagTarget.MapObject);
        foreach (var type in requiredTypes)
        {
            if (mapObject.Components.Any(c => c.GetType() == type)) continue;
            var empty = Activator.CreateInstance(type) as IMapObjectComponent;
            if (empty != null) mapObject.Components.Add(empty);
        }

        // Merge derived tags
        var derived = ResolveComponentTags(mapObject.Components.Select(c => c.GetType()), TagTarget.MapObject);
        foreach (var t in derived) mapObject.Tags.Add(t);

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

    private ContainerComponent? DeserializeContainerComponent(string data)
    {
        var dto = JsonSerializer.Deserialize<ContainerComponentDto>(data);
        return ContainerComponentMapper.FromDto(dto, _itemMapper);
    }

    private static IEnumerable<string> ResolveComponentTags(IEnumerable<Type> componentTypes, TagTarget target)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in componentTypes)
        {
            foreach (var def in TagDefinition.Predefined.Where(d => d.Target == target && d.ComponentType != null))
            {
                var t = def.ResolveComponentType();
                if (t != null && t.IsAssignableFrom(type))
                {
                    codes.Add(def.Code);
                    var colon = def.Code.IndexOf(':');
                    if (colon > 0) codes.Add(def.Code[(colon + 1)..]);
                }
            }
        }
        return codes;
    }
}
