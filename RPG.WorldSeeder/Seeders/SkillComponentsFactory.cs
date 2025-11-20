using System.Text.Json;
using RPG.Domain.Models.Skills; // ISkillComponent
using RPG.Domain.Models.Skills.SkillComponents;

namespace RPG.WorldSeeder.Seeders;

internal static class SkillComponentsFactory
{
    // Local safe deserialize to avoid missing extension binding issues
    private static T? SafeDeserializeLocal<T>(JsonElement element)
    {
        try { return JsonSerializer.Deserialize<T>(element.GetRawText()); } catch { return default; }
    }

    public static RPG.Domain.Models.Skills.ISkillComponent? Create(SkillComponentSeedModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Type)) return null;
        var type = model.Type.Trim().ToLowerInvariant();
        return type switch
        {
            "damage" => SafeDeserializeLocal<DamageComponent>(model.Properties),
            "cooldown" => SafeDeserializeLocal<CooldownComponent>(model.Properties),
            "requirement" => SafeDeserializeLocal<RequirementComponent>(model.Properties),
            "movement" => SafeDeserializeLocal<MovementComponent>(model.Properties),
            "buff" => SafeDeserializeLocal<BuffComponent>(model.Properties),
            "healing" => SafeDeserializeLocal<HealingComponent>(model.Properties),
            "healover" => SafeDeserializeLocal<HealOverTimeComponent>(model.Properties),
            "damageovertime" => SafeDeserializeLocal<DamageOverTimeComponent>(model.Properties),
            "shield" => SafeDeserializeLocal<ShieldComponent>(model.Properties),
            "resourcecost" => SafeDeserializeLocal<ResourceCostComponent>(model.Properties),
            "crowdcontrol" => SafeDeserializeLocal<CrowdControlComponent>(model.Properties),
            "areaofeffect" => SafeDeserializeLocal<AreaOfEffectComponent>(model.Properties),
            "casting" => SafeDeserializeLocal<CastingComponent>(model.Properties),
            "combo" => SafeDeserializeLocal<ComboComponent>(model.Properties),
            "debuff" => SafeDeserializeLocal<DebuffComponent>(model.Properties),
            _ => null
        };
    }
}
