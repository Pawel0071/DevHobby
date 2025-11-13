using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RPG.Domain.Enums;
using RPG.Infrastructure.Interfaces;
using RPG.Abstractions;
using RPG.Domain.Common;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Npcs.NpcComponents;
using RPG.Domain.Models.Skills;
using RPG.Infrastructure.Models;

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between Npc domain entity and NpcDocument.
///     Components are serialized to JSON for flexible storage.
/// </summary>
public class NpcModelMapper : IModelMapper<Npc, NpcDocument>
{
    private static readonly StringComparer TagComparer = StringComparer.OrdinalIgnoreCase;

    private readonly ILogger<NpcModelMapper> _logger;
    private readonly LocationMapper _locationMapper;
    private readonly IModelMapper<Skill, SkillDocument> _skillMapper;

    public NpcModelMapper(
        ILogger<NpcModelMapper> logger,
        LocationMapper locationMapper,
        IModelMapper<Skill, SkillDocument> skillMapper)
    {
        _logger = logger;
        _locationMapper = locationMapper;
        _skillMapper = skillMapper;
    }

    public NpcDocument ToPersistence(Npc entity)
    {
        _logger.Debug($"Converting Npc to NpcDocument. Id={entity.Id}, Name={entity.DisplayName}");

        EnsureComponentTags(entity);

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
            Tags = entity.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(TagComparer)
                .ToList(),
            BaseStats = entity.BaseStats.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
            ModifiedStats = entity.ModifiedStats.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
            Components = entity.Components
                .Select(component => SerializeComponent(component))
                .ToList()
        };
    }

    public Npc ToDomain(NpcDocument document)
    {
        _logger.Debug($"Converting NpcDocument to Npc. Id={document.Id}, Name={document.DisplayName}");

        var tagSet = CreateTagSet(document.Tags);
        var spawnLocation = _locationMapper.ToEntity(document.SpawnLocation);
        var npc = Npc.Create(document.Name, document.DisplayName, spawnLocation, document.WorldId, tagSet);

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

        // Deserialize explicit components from document
        foreach (var componentData in document.Components)
        {
            var component = DeserializeComponent(componentData);
            if (component != null)
            {
                npc.Components.Add(component);
            }
        }

        // Auto-add required components based on tags if missing
        var requiredTypes = TagComponentMap.GetRequiredComponentTypes(npc.Tags, TagTarget.Npc).ToList();
        foreach (var type in requiredTypes)
        {
            if (npc.Components.Any(c => c.GetType() == type)) continue;
            var empty = Activator.CreateInstance(type) as INpcComponent;
            if (empty != null) npc.Components.Add(empty);
        }

        EnsureComponentTags(npc);
        return npc;
    }

    // Backwards compatibility helper for existing usages
    public Npc ToEntity(NpcDocument document) => ToDomain(document);

    private ComponentData SerializeComponent(INpcComponent component)
    {
        return component switch
        {
            TrainerComponent trainer => SerializeTrainerComponent(trainer),
            CombatComponent combat => SerializeCombatComponent(combat),
            _ => new ComponentData
            {
                Type = component.GetType().Name,
                Data = JsonSerializer.Serialize(component, component.GetType())
            }
        };
    }

    private ComponentData SerializeTrainerComponent(TrainerComponent trainer)
    {
        var model = new TrainerComponentModel
        {
            Specialization = trainer.Specialization,
            TeachableSkills = trainer.TeachableSkills
                .Select(kvp => new SkillAvailabilityEntry
                {
                    Skill = _skillMapper.ToPersistence(kvp.Key),
                    Availability = kvp.Value
                })
                .ToList()
        };

        return new ComponentData
        {
            Type = nameof(TrainerComponent),
            Data = JsonSerializer.Serialize(model)
        };
    }

    private ComponentData SerializeCombatComponent(CombatComponent combat)
    {
        var model = new CombatComponentModel
        {
            AggroRange = combat.AggroRange,
            LeashRange = combat.LeashRange,
            AiBehaviorScript = combat.AiBehaviorScript,
            Stats = combat.Stats.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
            Skills = combat.Skills
                .Select(kvp => new SkillAvailabilityEntry
                {
                    Skill = _skillMapper.ToPersistence(kvp.Key),
                    Availability = kvp.Value
                })
                .ToList()
        };

        return new ComponentData
        {
            Type = nameof(CombatComponent),
            Data = JsonSerializer.Serialize(model)
        };
    }

    private INpcComponent? DeserializeComponent(ComponentData componentData)
    {
        return componentData.Type switch
        {
            nameof(CombatComponent) => DeserializeCombatComponent(componentData.Data),
            nameof(DialogueComponent) => JsonSerializer.Deserialize<DialogueComponent>(componentData.Data),
            nameof(LootableComponent) => JsonSerializer.Deserialize<LootableComponent>(componentData.Data),
            nameof(MerchantComponent) => JsonSerializer.Deserialize<MerchantComponent>(componentData.Data),
            nameof(QuestGiverComponent) => JsonSerializer.Deserialize<QuestGiverComponent>(componentData.Data),
            nameof(RespawnComponent) => JsonSerializer.Deserialize<RespawnComponent>(componentData.Data),
            nameof(TrainerComponent) => DeserializeTrainerComponent(componentData.Data),
            _ => null
        };
    }

    private CombatComponent? DeserializeCombatComponent(string json)
    {
        CombatComponentModel? model = null;

        try
        {
            model = JsonSerializer.Deserialize<CombatComponentModel>(json);
        }
        catch (NotSupportedException ex)
        {
            _logger.Debug($"Falling back to legacy combat component deserialization: {ex.Message}");
        }

        if (model is not null)
        {
            return BuildCombatComponent(model);
        }

        LegacyCombatComponentModel? legacyModel = null;
        try
        {
            legacyModel = JsonSerializer.Deserialize<LegacyCombatComponentModel>(json);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to deserialize combat component payload.", ex);
            return null;
        }

        if (legacyModel is null)
        {
            return null;
        }

        return BuildLegacyCombatComponent(legacyModel);
    }

    private TrainerComponent? DeserializeTrainerComponent(string json)
    {
        TrainerComponentModel? model = null;

        try
        {
            model = JsonSerializer.Deserialize<TrainerComponentModel>(json);
        }
        catch (NotSupportedException ex)
        {
            _logger.Debug($"Falling back to legacy trainer component deserialization: {ex.Message}");
        }

        if (model is not null)
        {
            return BuildTrainerComponent(model);
        }

        LegacyTrainerComponentModel? legacyModel = null;
        try
        {
            legacyModel = JsonSerializer.Deserialize<LegacyTrainerComponentModel>(json);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to deserialize trainer component payload.", ex);
            return null;
        }

        if (legacyModel is null)
        {
            return null;
        }

        return BuildLegacyTrainerComponent(legacyModel);
    }

    private CombatComponent BuildCombatComponent(CombatComponentModel model)
    {
        var component = new CombatComponent
        {
            AggroRange = model.AggroRange,
            LeashRange = model.LeashRange,
            AiBehaviorScript = model.AiBehaviorScript ?? string.Empty
        };

        ApplyStats(model.Stats, component);
        ApplySkillEntries(model.Skills, component);

        return component;
    }

    private CombatComponent BuildLegacyCombatComponent(LegacyCombatComponentModel model)
    {
        var component = new CombatComponent
        {
            AggroRange = model.AggroRange,
            LeashRange = model.LeashRange,
            AiBehaviorScript = model.AiBehaviorScript ?? string.Empty
        };

        ApplyStats(model.Stats, component);
        ApplyLegacySkillEntries(model.Skills, component);

        return component;
    }

    private TrainerComponent BuildTrainerComponent(TrainerComponentModel model)
    {
        var component = new TrainerComponent
        {
            Specialization = model.Specialization ?? string.Empty
        };

        ApplyTrainerSkills(model.TeachableSkills, component);
        return component;
    }

    private TrainerComponent BuildLegacyTrainerComponent(LegacyTrainerComponentModel model)
    {
        var component = new TrainerComponent
        {
            Specialization = model.Specialization ?? string.Empty
        };

        ApplyLegacyTrainerSkills(model.TeachableSkills, component);
        return component;
    }

    private static void ApplyStats(Dictionary<string, int>? stats, CombatComponent component)
    {
        if (stats is null)
        {
            return;
        }

        foreach (var stat in stats)
        {
            if (Enum.TryParse<StatsProperty>(stat.Key, out var statProperty))
            {
                component.Stats[statProperty] = stat.Value;
            }
        }
    }

    private void ApplySkillEntries(IEnumerable<SkillAvailabilityEntry>? entries, CombatComponent component)
    {
        if (entries is null)
        {
            return;
        }

        foreach (var entry in entries)
        {
            if (entry?.Skill is null)
            {
                continue;
            }

            var skill = _skillMapper.ToDomain(entry.Skill);
            component.Skills[skill] = entry.Availability;
        }
    }

    private static void ApplyLegacySkillEntries(IEnumerable<LegacySkillAvailabilityEntry>? entries, CombatComponent component)
    {
        if (entries is null)
        {
            return;
        }

        foreach (var entry in entries)
        {
            if (entry?.Skill is null)
            {
                continue;
            }

            component.Skills[entry.Skill] = entry.Availability;
        }
    }

    private void ApplyTrainerSkills(IEnumerable<SkillAvailabilityEntry>? entries, TrainerComponent component)
    {
        if (entries is null)
        {
            return;
        }

        foreach (var entry in entries)
        {
            if (entry?.Skill is null)
            {
                continue;
            }

            var skill = _skillMapper.ToDomain(entry.Skill);
            component.TeachableSkills[skill] = entry.Availability;
        }
    }

    private static void ApplyLegacyTrainerSkills(IEnumerable<LegacySkillAvailabilityEntry>? entries, TrainerComponent component)
    {
        if (entries is null)
        {
            return;
        }

        foreach (var entry in entries)
        {
            if (entry?.Skill is null)
            {
                continue;
            }

            component.TeachableSkills[entry.Skill] = entry.Availability;
        }
    }

    private sealed class SkillAvailabilityEntry
    {
        public SkillAvailability Availability { get; set; }
        public SkillDocument? Skill { get; set; }
    }

    private sealed class CombatComponentModel
    {
        public float AggroRange { get; set; }
        public string? AiBehaviorScript { get; set; }
        public float LeashRange { get; set; }
        public List<SkillAvailabilityEntry>? Skills { get; set; }
        public Dictionary<string, int>? Stats { get; set; }
    }

    private sealed class TrainerComponentModel
    {
        public string? Specialization { get; set; }
        public List<SkillAvailabilityEntry>? TeachableSkills { get; set; }
    }

    private sealed class LegacySkillAvailabilityEntry
    {
        public SkillAvailability Availability { get; set; }
        public Skill? Skill { get; set; }
    }

    private sealed class LegacyCombatComponentModel
    {
        public float AggroRange { get; set; }
        public string? AiBehaviorScript { get; set; }
        public float LeashRange { get; set; }
        public List<LegacySkillAvailabilityEntry>? Skills { get; set; }
        public Dictionary<string, int>? Stats { get; set; }
    }

    private sealed class LegacyTrainerComponentModel
    {
        public string? Specialization { get; set; }
        public List<LegacySkillAvailabilityEntry>? TeachableSkills { get; set; }
    }

    private static void EnsureComponentTags(Npc npc)
    {
        npc.Tags = CreateTagSet(npc.Tags);

        // Add tags derived from component types via helper
        var resolved = TagComponentHelper.ResolveComponentTags(npc.Components.Select(c => c.GetType()), TagTarget.Npc);
        foreach (var tag in resolved)
        {
            if (!string.IsNullOrWhiteSpace(tag)) npc.Tags.Add(tag.Trim());
        }

        npc.Tags.RemoveWhere(static tag => string.IsNullOrWhiteSpace(tag));
    }

    private static IEnumerable<string> ResolveComponentTags(Type componentType)
    {
        // Legacy method kept for backward compatibility (used nowhere now) -> return empty.
        return Array.Empty<string>();
    }

    private static HashSet<string> CreateTagSet(IEnumerable<string>? tags = null)
    {
        var set = new HashSet<string>(TagComparer);
        if (tags == null)
        {
            return set;
        }

        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            set.Add(tag.Trim());
        }

        return set;
    }
}
