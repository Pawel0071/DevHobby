using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Core.Common;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Domain.Models;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Items.ItemComponent;
using RPG.Domain.Models.Skills;
using RPG.Domain.Models.Skills.SkillComponents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.SkillService;

public class SkillService : ISkillService
{
    private readonly ILogger<SkillService> _logger;

    public SkillService(ILogger<SkillService> logger)
    {
        _logger = logger;
    }

    public ServiceResult<bool> AddSkillsAfterLevelUp(Character character)
    {
        RefreshSkillAvailability(character);
        _logger.Info($"Re-evaluated skills after level up for character '{character.Id}'.");
        return true.ToResult();
    }

    public ServiceResult<bool> AddSkillsAfterEquipItem(Character character, Item item)
    {
        RefreshSkillAvailability(character);
        ApplyGrantedSkillsAvailability(character, item, SkillAvailability.Available);
        _logger.Info($"Updated skills after equipping item '{item.Name}' for character '{character.Id}'.");
        return true.ToResult();
    }

    public ServiceResult<bool> RemoveSkillsAfterUnEquipItem(Character character, Item item)
    {
        RefreshSkillAvailability(character);
        ApplyGrantedSkillsAvailability(character, item, SkillAvailability.UnAvailable);
        _logger.Info($"Updated skills after unequipping item '{item.Name}' for character '{character.Id}'.");
        return true.ToResult();
    }

    public ServiceResult<bool> UseSkill(Character character, Skill skill)
    {
        var skillsContainer = character.GetSkillsContainer();

    if (!TryGetSkillEntry(skillsContainer, skill, out var trackedSkill, out var availability))
        {
            _logger.Warn($"Character '{character.Id}' attempted to use unknown skill '{skill.Name}'.");
            return ErrorCodeDefinition.SkillNotKnown.ToFail<bool>("Postać nie zna tej umiejętności.");
        }

        if (availability == SkillAvailability.UnAvailable)
        {
            _logger.Warn($"Skill '{skill.Name}' is unavailable for character '{character.Id}'.");
            return ErrorCodeDefinition.SkillUnavailable.ToFail<bool>("Umiejętność jest niedostępna.");
        }

        var resourceCheck = EnsureResourceAvailability(character, trackedSkill, useCurrentResource: true);
        if (!resourceCheck.Success)
        {
            _logger.Warn($"Character '{character.Id}' lacks resources for skill '{skill.Name}'.");
            return resourceCheck;
        }

    ConsumeResources(character, trackedSkill);
    skillsContainer.ActiveSkills[trackedSkill] = DateTime.UtcNow;

        _logger.Info($"Character '{character.Id}' used skill '{skill.Name}'.");
        return true.ToResult();
    }

    public ServiceResult<bool> LearnSkill(Character character, Skill skill)
    {
        var skillsContainer = character.GetSkillsContainer();

    if (skillsContainer.Skills.Keys.Any(existing => existing.Id == skill.Id))
        {
            _logger.Warn($"Character '{character.Id}' already knows skill '{skill.Name}'.");
            return ErrorCodeDefinition.SkillAlreadyKnown.ToFail<bool>("Umiejętność jest już znana.");
        }

        var requirementCheck = EnsureSkillPrerequisites(character, skill, requireResourceCapacity: true);
        if (!requirementCheck.Success)
        {
            return requirementCheck;
        }

    skillsContainer.Skills[skill] = SkillAvailability.Available;
        _logger.Info($"Character '{character.Id}' learned skill '{skill.Name}'.");
        return true.ToResult();
    }

    public ServiceResult<bool> UnlearnSkill(Character character, Skill skill)
    {
        var skillsContainer = character.GetSkillsContainer();

        if (!TryGetSkillEntry(skillsContainer, skill, out var trackedSkill, out _))
        {
            _logger.Warn($"Character '{character.Id}' cannot unlearn unknown skill '{skill.Name}'.");
            return ErrorCodeDefinition.SkillNotKnown.ToFail<bool>("Umiejętność nie jest znana.");
        }

    skillsContainer.Skills.Remove(trackedSkill);
    skillsContainer.ActiveSkills.Remove(trackedSkill);

        _logger.Info($"Character '{character.Id}' unlearned skill '{skill.Name}'.");
        return true.ToResult();
    }

    private void RefreshSkillAvailability(Character character)
    {
        var skillsContainer = character.GetSkillsContainer();
        var snapshot = skillsContainer.Skills.Keys.ToList();

        foreach (var trackedSkill in snapshot)
        {
            var requirementCheck = EnsureSkillPrerequisites(character, trackedSkill, requireResourceCapacity: true);
            if (requirementCheck.Success)
            {
                skillsContainer.Skills[trackedSkill] = SkillAvailability.Available;
            }
            else
            {
                skillsContainer.Skills[trackedSkill] = SkillAvailability.UnAvailable;
                skillsContainer.ActiveSkills.Remove(trackedSkill);
            }
        }
    }

    private void ApplyGrantedSkillsAvailability(Character character, Item item, SkillAvailability availability)
    {
        var grantComponent = item.GetComponent<SkillGrantComponent>();
        if (grantComponent?.SkillIds is not { Count: > 0 })
        {
            return;
        }

        var skillsContainer = character.GetSkillsContainer();

        foreach (var skillId in grantComponent.SkillIds)
        {
            if (!TryGetSkillById(skillsContainer, skillId, out var grantedSkill) || grantedSkill is null)
            {
                continue;
            }

            if (availability == SkillAvailability.Available)
            {
                var requirementCheck = EnsureSkillPrerequisites(character, grantedSkill, requireResourceCapacity: true);
                if (!requirementCheck.Success)
                {
                    continue;
                }
            }

            skillsContainer.Skills[grantedSkill] = availability;

            if (availability == SkillAvailability.UnAvailable)
            {
                skillsContainer.ActiveSkills.Remove(grantedSkill);
            }
        }
    }

    private ServiceResult<bool> EnsureSkillPrerequisites(Character character, Skill skill, bool requireResourceCapacity)
    {
        var requirementComponent = skill.GetComponent<RequirementComponent>();
        if (requirementComponent != null)
        {
            if (character.Level < requirementComponent.RequiredLevel)
            {
                return ErrorCodeDefinition.SkillRequirementLevelNotMet.ToFail<bool>("Zbyt niski poziom postaci.");
            }

            if (requirementComponent.RequiredClasses is { Count: > 0 })
            {
                var className = character.Class.ToString();
                var matchesClass = requirementComponent.RequiredClasses.Any(requiredClass =>
                    string.Equals(requiredClass, className, StringComparison.OrdinalIgnoreCase));

                if (!matchesClass)
                {
                    return ErrorCodeDefinition.SkillRequirementClassMismatch.ToFail<bool>("Postać nie spełnia wymagań klasy.");
                }
            }

            if (requirementComponent.RequiredWeaponTypes is { Count: > 0 })
            {
                if (!HasRequiredWeaponEquipped(character, requirementComponent.RequiredWeaponTypes))
                {
                    return ErrorCodeDefinition.SkillRequirementWeaponMissing.ToFail<bool>("Brak wymaganego uzbrojenia.");
                }
            }

            if (requirementComponent.RequiresMeleeWeapon &&
                !HasRequiredWeaponEquipped(character, new[] { "melee" }))
            {
                return ErrorCodeDefinition.SkillRequirementWeaponMissing.ToFail<bool>("Brak broni do walki wręcz.");
            }

            if (requirementComponent.RequiresRangedWeapon &&
                !HasRequiredWeaponEquipped(character, new[] { "ranged", "bow", "crossbow", "gun" }))
            {
                return ErrorCodeDefinition.SkillRequirementWeaponMissing.ToFail<bool>("Brak broni dystansowej.");
            }

            if (requirementComponent.RequiredStats is { Count: > 0 })
            {
                foreach (var (statKey, requiredValue) in requirementComponent.RequiredStats)
                {
                    if (!Enum.TryParse<StatsProperty>(statKey, true, out var statProperty))
                    {
                        continue;
                    }

                    var currentValue = GetStatValue(character, statProperty);
                    if (currentValue < requiredValue)
                    {
                        return ErrorCodeDefinition.SkillRequirementStatNotMet.ToFail<bool>("Zbyt niskie atrybuty.");
                    }
                }
            }

            if (requirementComponent.RequiredSkillIds is { Count: > 0 })
            {
                var skillsContainer = character.GetSkillsContainer();
                foreach (var requiredSkillId in requirementComponent.RequiredSkillIds)
                {
                    if (!TryGetSkillById(skillsContainer, requiredSkillId, out _))
                    {
                        return ErrorCodeDefinition.SkillPrerequisiteMissing.ToFail<bool>("Brak wymaganych umiejętności.");
                    }
                }
            }
        }

        if (requireResourceCapacity)
        {
            var resourceCheck = EnsureResourceAvailability(character, skill, useCurrentResource: false);
            if (!resourceCheck.Success)
            {
                return resourceCheck;
            }
        }

        return true.ToResult();
    }

    private ServiceResult<bool> EnsureResourceAvailability(Character character, Skill skill, bool useCurrentResource)
    {
        var resourceComponent = skill.GetComponent<ResourceCostComponent>();
        if (resourceComponent?.Costs is not { Count: > 0 })
        {
            return true.ToResult();
        }

        var totalCost = resourceComponent.Costs.Values.Sum();
        var resourcePool = useCurrentResource ? character.CurrentResource : character.MaxResource;

        if (resourcePool < totalCost)
        {
            return ErrorCodeDefinition.SkillRequirementResourceInsufficient.ToFail<bool>("Postać nie posiada wystarczających zasobów.");
        }

        return true.ToResult();
    }

    private void ConsumeResources(Character character, Skill skill)
    {
        var resourceComponent = skill.GetComponent<ResourceCostComponent>();
        if (resourceComponent?.Costs is not { Count: > 0 } || !resourceComponent.ConsumeOnCast)
        {
            return;
        }

        var totalCost = resourceComponent.Costs.Values.Sum();
        character.CurrentResource = Math.Max(0, character.CurrentResource - totalCost);
    }

    private static bool HasRequiredWeaponEquipped(Character character, IEnumerable<string> requiredTypes)
    {
        var equippedWeapons = character.Equipments
            .Where(kvp => kvp.Key is EquipmentSlot.Weapon1 or EquipmentSlot.Weapon2)
            .Select(kvp => kvp.Value)
            .Where(item => item != null)
            .ToArray();

        if (!equippedWeapons.Any())
        {
            return false;
        }

        foreach (var required in requiredTypes)
        {
            if (string.IsNullOrWhiteSpace(required))
            {
                continue;
            }

            var normalizedRequired = Normalize(required);

            var matches = equippedWeapons.Any(item => item != null && MatchesWeaponType(item, normalizedRequired));
            if (matches)
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesWeaponType(Item item, string normalizedRequired)
    {
        if (MatchesString(item.TypeCode, normalizedRequired))
        {
            return true;
        }

        if (MatchesString(item.Name, normalizedRequired))
        {
            return true;
        }

        if (item.Tags != null && item.Tags.Any(tag => MatchesString(tag, normalizedRequired)))
        {
            return true;
        }

        return false;
    }

    private static bool MatchesString(string? value, string normalizedReference)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalizedValue = Normalize(value);
        return normalizedValue.Contains(normalizedReference, StringComparison.OrdinalIgnoreCase) ||
               normalizedReference.Contains(normalizedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        return value
            .Trim()
            .Replace("item:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("weapon:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("skill:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace('-', ' ')
            .ToLowerInvariant();
    }

    private static int GetStatValue(Character character, StatsProperty statProperty)
    {
        if (character.ModifiedStats.TryGetValue(statProperty, out var modifiedValue))
        {
            return modifiedValue;
        }

        if (character.BaseStats.TryGetValue(statProperty, out var baseValue))
        {
            return baseValue;
        }

        return 0;
    }

    private static bool TryGetSkillEntry(ISkillsContainer container, Skill skill, out Skill trackedSkill, out SkillAvailability availability)
    {
        if (container.Skills.TryGetValue(skill, out availability))
        {
            trackedSkill = skill;
            return true;
        }

        trackedSkill = container.Skills.Keys.FirstOrDefault(s => s.Id == skill.Id) ?? skill;
        if (container.Skills.TryGetValue(trackedSkill, out availability))
        {
            return true;
        }

        availability = SkillAvailability.UnAvailable;
        return false;
    }

    private static bool TryGetSkillById(ISkillsContainer container, Guid id, out Skill? skill)
    {
        skill = container.Skills.Keys.FirstOrDefault(s => s.Id == id);
        return skill != null;
    }
}
