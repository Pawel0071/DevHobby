using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Domain.Entities.Skills;
using RPG.Domain.Entities.Skills.SkillComponents;

namespace RPG.Core.Common;

/// <summary>
///     Provides predefined combat skills that can be shared between characters and NPCs.
///     Skills expose deterministic identifiers so that persisted documents and runtime state stay aligned.
/// </summary>
public static class StarterCombatSkills
{
    public static StarterSkillSet CreateDefaultSet()
    {
        var basicAttack = BuildBasicAttack();
        var powerAttack = BuildPowerAttack();
        var ultimate = BuildUltimate();

        return new StarterSkillSet(basicAttack, powerAttack, ultimate);
    }

    private static Skill BuildBasicAttack()
    {
        var skill = CreateSkill(
            id: Guid.Parse("7e3e2f0a-2b61-44f4-9437-01610c2a2792"),
            name: "Basic Attack",
            description: "A quick melee strike that every adventurer learns on day one.");

        skill.IconId = "icon_skill_basic_attack";
        skill.Tags.UnionWith(new[] { "basic-attack", "melee", "starter" });

        skill.Components.Add(new DamageComponent
        {
            BaseDamage = 14,
            MinDamage = 12,
            MaxDamage = 16,
            ScalingFactor = 1.0f,
            ScalingStat = "strength",
            DamageType = "physical",
            CritMultiplier = 1.8f
        });

        skill.Components.Add(new CooldownComponent
        {
            CooldownSeconds = 2,
            UseGlobalCooldown = true,
            GlobalCooldownMs = 1000
        });

        skill.Components.Add(new RequirementComponent
        {
            RequiredLevel = 1
        });

        return skill;
    }

    private static Skill BuildPowerAttack()
    {
        var skill = CreateSkill(
            id: Guid.Parse("22f5a2be-62ee-4de3-840d-0a8d7e3c9c0f"),
            name: "Power Attack",
            description: "A heavy overhead swing that pushes foes back.");

        skill.IconId = "icon_skill_power_attack";
        skill.Tags.UnionWith(new[] { "power-attack", "melee", "starter" });

        skill.Components.Add(new DamageComponent
        {
            BaseDamage = 34,
            MinDamage = 30,
            MaxDamage = 38,
            ScalingFactor = 1.35f,
            ScalingStat = "strength",
            DamageType = "physical",
            CritMultiplier = 2.2f
        });

        skill.Components.Add(new CooldownComponent
        {
            CooldownSeconds = 6,
            UseGlobalCooldown = true,
            GlobalCooldownMs = 1500
        });

        skill.Components.Add(new RequirementComponent
        {
            RequiredLevel = 4
        });

        return skill;
    }

    private static Skill BuildUltimate()
    {
        var skill = CreateSkill(
            id: Guid.Parse("f0c6b7c2-5d49-4c45-8e58-9e6aa4a3d482"),
            name: "Ultimate",
            description: "A signature finishing move reserved for true heroes.");

        skill.IconId = "icon_skill_ultimate";
        skill.Tags.UnionWith(new[] { "ultimate", "melee", "finisher" });

        skill.Components.Add(new DamageComponent
        {
            BaseDamage = 78,
            MinDamage = 62,
            MaxDamage = 96,
            ScalingFactor = 1.8f,
            ScalingStat = "strength",
            DamageType = "physical",
            CritMultiplier = 2.5f
        });

        skill.Components.Add(new CooldownComponent
        {
            CooldownSeconds = 30,
            UseGlobalCooldown = false,
            MaxCharges = 1
        });

        skill.Components.Add(new RequirementComponent
        {
            RequiredLevel = 8
        });

        return skill;
    }

    private static Skill CreateSkill(Guid id, string name, string description)
    {
        var skill = Skill.Create(name, description);
        typeof(Skill).GetProperty("Id")!.SetValue(skill, id);
        return skill;
    }
}

public sealed record StarterSkillSet(Skill BasicAttack, Skill PowerAttack, Skill Ultimate)
{
    public IReadOnlyCollection<Skill> All { get; } = new[] { BasicAttack, PowerAttack, Ultimate };
}
