using System.Text.Json;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Entities.Npcs.NpcComponents;
using RPG.Domain.Enums;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between Npc domain entity and NpcDocument.
///     Components are serialized to JSON for flexible storage.
/// </summary>
public class NpcDocumentMapper : IDocumentMapper<Npc, NpcDocument>
{
    private readonly ILogger<NpcDocumentMapper> _logger;
    private readonly LocationMapper _locationMapper;

    public NpcDocumentMapper(ILogger<NpcDocumentMapper> logger, LocationMapper locationMapper)
    {
        _logger = logger;
        _locationMapper = locationMapper;
    }

    public NpcDocument ToDocument(Npc entity)
    {
        _logger.Debug($"Converting Npc to NpcDocument. Id={entity.Id}, Name={entity.DisplayName}");

        return new NpcDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            Level = entity.Level,
            CurrentHealth = entity.CurrentHealth,
            MaxHealth = entity.MaxHealth,
            SpawnLocation = _locationMapper.ToDocument(entity.SpawnLocation),
            CurrentLocation = _locationMapper.ToDocument(entity.CurrentLocation ?? entity.SpawnLocation),
            IsMoving = entity.IsMoving,
            IsRotating = entity.IsRotating,
            WorldId = entity.WorldId,
            Tags = entity.Tags.ToList(),
            BaseStats = entity.BaseStats.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
            ModifiedStats = entity.ModifiedStats.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
            Components = entity.Components.Select(component => new ComponentData
            {
                Type = component.GetType().Name,
                Data = JsonSerializer.Serialize(component, component.GetType())
            }).ToList()
        };
    }

    public Npc ToDomain(NpcDocument document)
    {
        _logger.Debug($"Converting NpcDocument to Npc. Id={document.Id}, Name={document.DisplayName}");

        var spawnLocation = _locationMapper.ToEntity(document.SpawnLocation);
        var npc = Npc.Create(document.Name, document.DisplayName, spawnLocation, document.WorldId, document.Tags.ToHashSet());

        // Preserve identity using reflection helpers similar to other mappers
        typeof(Npc).GetProperty("Id")!.SetValue(npc, document.Id);

        npc.Description = document.Description;
        npc.Level = document.Level;
        npc.CurrentHealth = document.CurrentHealth;
        npc.MaxHealth = document.MaxHealth;

        if (document.CurrentLocation is not null)
        {
            npc.SetCurrentLocation(_locationMapper.ToEntity(document.CurrentLocation));
        }
        else
        {
            npc.SetCurrentLocation(spawnLocation);
        }
        npc.SetMovementState(document.IsMoving);
        npc.SetRotationState(document.IsRotating);
        npc.Components.Clear();

        if (document.BaseStats is not null)
        {
            foreach (var stat in document.BaseStats)
            {
                if (Enum.TryParse<StatsProperty>(stat.Key, out var statProperty))
                {
                    npc.BaseStats[statProperty] = stat.Value;
                }
            }
        }

        if (document.ModifiedStats is not null)
        {
            foreach (var stat in document.ModifiedStats)
            {
                if (Enum.TryParse<StatsProperty>(stat.Key, out var statProperty))
                {
                    npc.ModifiedStats[statProperty] = stat.Value;
                }
            }
        }

        foreach (var componentData in document.Components)
        {
            var component = DeserializeComponent(componentData);
            if (component != null)
            {
                npc.Components.Add(component);
            }
        }

        return npc;
    }

    // Backwards compatibility helper for existing usages
    public Npc ToEntity(NpcDocument document) => ToDomain(document);

    private static INpcComponent? DeserializeComponent(ComponentData componentData)
    {
        return componentData.Type switch
        {
            nameof(CombatComponent) => JsonSerializer.Deserialize<CombatComponent>(componentData.Data),
            nameof(DialogueComponent) => JsonSerializer.Deserialize<DialogueComponent>(componentData.Data),
            nameof(LootableComponent) => JsonSerializer.Deserialize<LootableComponent>(componentData.Data),
            nameof(MerchantComponent) => JsonSerializer.Deserialize<MerchantComponent>(componentData.Data),
            nameof(QuestGiverComponent) => JsonSerializer.Deserialize<QuestGiverComponent>(componentData.Data),
            nameof(RespawnComponent) => JsonSerializer.Deserialize<RespawnComponent>(componentData.Data),
            nameof(TrainerComponent) => JsonSerializer.Deserialize<TrainerComponent>(componentData.Data),
            _ => null
        };
    }
}
