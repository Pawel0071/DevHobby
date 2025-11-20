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
                .Select(SerializeComponent)
                .ToList(),
            Skills = entity.Skills.ToDictionary(
                kvp => kvp.Key.Id.ToString(),
                kvp => kvp.Value.ToString()),
            ActiveSkills = entity.ActiveSkills.ToDictionary(
                kvp => kvp.Key.Id.ToString(),
                kvp => kvp.Value)
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

        npc.SetCurrentLocation(document.CurrentLocation is null
            ? spawnLocation
            : _locationMapper.ToEntity(document.CurrentLocation));
        npc.SetMovementState(document.IsMoving);
        npc.SetRotationState(document.IsRotating);

        npc.Components.Clear();
        foreach (var componentData in document.Components)
        {
            var component = DeserializeComponent(componentData);
            if (component != null)
            {
                npc.Components.Add(component);
            }
        }

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

        npc.Skills.Clear();
        if (document.Skills is not null)
        {
            foreach (var entry in document.Skills)
            {
                if (!Guid.TryParse(entry.Key, out var skillId))
                {
                    continue;
                }

                if (!Enum.TryParse<SkillAvailability>(entry.Value, out var availability))
                {
                    continue;
                }

                var skillDoc = new SkillDocument { Id = skillId, Name = string.Empty };
                var skill = _skillMapper.ToDomain(skillDoc);
                npc.Skills[skill] = availability;
            }
        }

        npc.ActiveSkills.Clear();
        if (document.ActiveSkills is not null)
        {
            foreach (var entry in document.ActiveSkills)
            {
                if (!Guid.TryParse(entry.Key, out var skillId))
                {
                    continue;
                }

                var skillDoc = new SkillDocument { Id = skillId, Name = string.Empty };
                var skill = _skillMapper.ToDomain(skillDoc);
                npc.ActiveSkills[skill] = entry.Value;
            }
        }

        // Auto-add required components based on tags if missing
        var requiredTypes = TagComponentMap.GetRequiredComponentTypes(npc.Tags, TagTarget.Npc).ToList();
        foreach (var type in requiredTypes)
        {
            if (npc.Components.Any(c => c.GetType() == type))
            {
                continue;
            }

            if (Activator.CreateInstance(type) is INpcComponent empty)
            {
                npc.Components.Add(empty);
            }
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
            AiBehaviorScript = combat.AiBehaviorScript
        };

        return new ComponentData
        {
            Type = nameof(CombatComponent),
            Data = JsonSerializer.Serialize(model)
        };
    }

    public INpcComponent? DeserializeComponent(ComponentData componentData)
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
        try
        {
            var model = JsonSerializer.Deserialize<CombatComponentModel>(json);
            if (model is not null)
            {
                return BuildCombatComponent(model);
            }
        }
        catch (NotSupportedException ex)
        {
            _logger.Debug($"Falling back to legacy combat component deserialization: {ex.Message}");
        }

        try
        {
            var legacyModel = JsonSerializer.Deserialize<LegacyCombatComponentModel>(json);
            return legacyModel is null ? null : BuildLegacyCombatComponent(legacyModel);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to deserialize combat component payload.", ex);
            return null;
        }
    }

    private TrainerComponent? DeserializeTrainerComponent(string json)
    {
        try
        {
            var model = JsonSerializer.Deserialize<TrainerComponentModel>(json);
            if (model is not null)
            {
                return BuildTrainerComponent(model);
            }
        }
        catch (NotSupportedException ex)
        {
            _logger.Debug($"Falling back to legacy trainer component deserialization: {ex.Message}");
        }

        try
        {
            var legacyModel = JsonSerializer.Deserialize<LegacyTrainerComponentModel>(json);
            return legacyModel is null ? null : BuildLegacyTrainerComponent(legacyModel);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to deserialize trainer component payload.", ex);
            return null;
        }
    }

    private CombatComponent BuildCombatComponent(CombatComponentModel model)
    {
        return new CombatComponent
        {
            AggroRange = model.AggroRange,
            LeashRange = model.LeashRange,
            AiBehaviorScript = model.AiBehaviorScript ?? string.Empty
        };
    }

    private CombatComponent BuildLegacyCombatComponent(LegacyCombatComponentModel model)
    {
        var component = new CombatComponent
        {
            AggroRange = model.AggroRange,
            LeashRange = model.LeashRange,
            AiBehaviorScript = model.AiBehaviorScript ?? string.Empty
        };

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
