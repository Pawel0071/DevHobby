using System.Text.Json;
using RPG.Infrastructure.Interfaces;
using RPG.Domain.Enums; // for TagTarget
using RPG.Abstractions;
using RPG.Domain.Models.Skills;
using RPG.Domain.Models.Skills.SkillComponents;
using RPG.Infrastructure.Models;

// tag component map + helper

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between Skill domain entity and SkillDocument
///     Components are serialized to JSON for flexible storage
/// </summary>
public class SkillModelMapper : IModelMapper<Skill, SkillDocument>
{
    private readonly ILogger<SkillModelMapper> _logger;

    public SkillModelMapper(ILogger<SkillModelMapper> logger)
    {
        _logger = logger;
    }

    public SkillDocument ToPersistence(Skill entity)
    {
        _logger.Debug($"Converting Skill to SkillDocument. Id={entity.Id}, Name={entity.Name}");
        // synchronize tags from components before persisting (merge, don't override)
        var derived = TagComponentHelper.ResolveComponentTags(entity.Components.Select(c => c.GetType()), TagTarget.Skill);
        foreach (var t in derived) entity.Tags.Add(t);
        return new SkillDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IconId = entity.IconId,
            Tags = entity.Tags.ToList(),
            Components = entity.Components.Select(c => new ComponentData
            {
                Type = c.GetType().Name, Data = JsonSerializer.Serialize(c, c.GetType())
            }).ToList()
        };
    }

    public Skill ToDomain(SkillDocument document)
    {
        _logger.Debug($"Converting SkillDocument to Skill. Id={document.Id}, Name={document.Name}");
        var skill = Skill.Create(document.Name, document.Description);

        // Preserve ID from document using reflection
        typeof(Skill).GetProperty("Id")!.SetValue(skill, document.Id);

        skill.IconId = document.IconId;
        skill.Tags = document.Tags.ToHashSet();

        // Deserialize components
        foreach (var componentData in document.Components)
        {
            var component = DeserializeComponent(componentData);
            if (component != null) skill.Components.Add(component);
        }

        // Auto-add missing components based on tags
        var requiredTypes = TagComponentMap.GetRequiredComponentTypes(skill.Tags, TagTarget.Skill);
        foreach (var type in requiredTypes)
        {
            if (skill.Components.Any(c => c.GetType() == type)) continue;
            var empty = Activator.CreateInstance(type) as ISkillComponent;
            if (empty != null) skill.Components.Add(empty);
        }

        // Ensure tags reflect present components (merge with existing document tags)
        var resolved = TagComponentHelper.ResolveComponentTags(skill.Components.Select(c => c.GetType()), TagTarget.Skill);
        foreach (var t in resolved) skill.Tags.Add(t);

        return skill;
    }

    public Skill ToEntity(SkillDocument document) => ToDomain(document);

    private ISkillComponent? DeserializeComponent(ComponentData componentData)
    {
        return componentData.Type switch
        {
            // Damage & Healing
            nameof(DamageComponent) => JsonSerializer.Deserialize<DamageComponent>(componentData.Data),
            nameof(HealingComponent) => JsonSerializer.Deserialize<HealingComponent>(componentData.Data),
            nameof(DamageOverTimeComponent) => JsonSerializer.Deserialize<DamageOverTimeComponent>(componentData.Data),
            nameof(HealOverTimeComponent) => JsonSerializer.Deserialize<HealOverTimeComponent>(componentData.Data),

            // Buffs & Debuffs
            nameof(BuffComponent) => JsonSerializer.Deserialize<BuffComponent>(componentData.Data),
            nameof(DebuffComponent) => JsonSerializer.Deserialize<DebuffComponent>(componentData.Data),

            // Control & Movement
            nameof(CrowdControlComponent) => JsonSerializer.Deserialize<CrowdControlComponent>(componentData.Data),
            nameof(MovementComponent) => JsonSerializer.Deserialize<MovementComponent>(componentData.Data),

            // Defense
            nameof(ShieldComponent) => JsonSerializer.Deserialize<ShieldComponent>(componentData.Data),

            // Targeting
            nameof(AreaOfEffectComponent) => JsonSerializer.Deserialize<AreaOfEffectComponent>(componentData.Data),

            // Mechanics
            nameof(ResourceCostComponent) => JsonSerializer.Deserialize<ResourceCostComponent>(componentData.Data),
            nameof(CastingComponent) => JsonSerializer.Deserialize<CastingComponent>(componentData.Data),
            nameof(CooldownComponent) => JsonSerializer.Deserialize<CooldownComponent>(componentData.Data),
            nameof(RequirementComponent) => JsonSerializer.Deserialize<RequirementComponent>(componentData.Data),
            nameof(ComboComponent) => JsonSerializer.Deserialize<ComboComponent>(componentData.Data),

            _ => null
        };
    }
}
