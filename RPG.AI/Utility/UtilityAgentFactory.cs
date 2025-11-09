using System;
using System.Collections.Generic;
using System.Linq;
using RPG.AI.Utility.Actions;
using RPG.Domain.Entities.Npcs.NpcComponents;
using RPG.Domain.Entities.Skills;

namespace RPG.AI.Utility;

public static class UtilityAgentFactory
{
    public static UtilityAgent CreateAggressiveMelee(Skill basicAttack, UtilityAgentSettings? settings = null)
    {
        settings ??= UtilityAgentSettings.Default;

        return new UtilityAgent("aggressive-melee")
            .Register(UtilityActionCatalog.UseSkill(
                "basic-attack",
                basicAttack,
                settings.MeleeRange,
                settings.MeleeMaxRange,
                settings.BasicAttackCooldown,
                weight: 6f))
            .Register(UtilityActionCatalog.FollowTarget(
                "follow-target",
                settings.MeleeRange,
                settings.MeleeStopDistance,
                settings.ChaseRange,
                weight: 4f))
            .Register(UtilityActionCatalog.AcquireTarget(
                "acquire-target",
                settings.AggroRadius,
                weight: 3f))
            .Register(UtilityActionCatalog.ReturnToSpawn(
                "return-to-spawn",
                settings.ReturnToSpawnTolerance,
                weight: 2f))
            .Register(UtilityActionCatalog.Patrol(
                "patrol-route",
                settings.PatrolRadius,
                settings.PatrolWaypointCount,
                settings.PatrolStopDistance,
                settings.PatrolDwellTime,
                weight: 1.5f))
            .Register(UtilityActionCatalog.Idle("idle", settings.IdleAnimation, weight: 0.5f));
    }

    public static UtilityAgent CreateDefensiveHealer(Skill heal, Skill basicAttack, UtilityAgentSettings? settings = null)
    {
        settings ??= UtilityAgentSettings.Default;

        return new UtilityAgent("defensive-healer")
            .Register(UtilityActionCatalog.UseSkill(
                "heal-self",
                heal,
                settings.SelfHealOptimalRange,
                settings.SelfHealMaxRange,
                settings.HealCooldown,
                weight: 7f))
            .Register(UtilityActionCatalog.UseSkill(
                "basic-attack",
                basicAttack,
                settings.RangedIdealRange,
                settings.RangedMaxRange,
                settings.BasicAttackCooldown,
                weight: 5f))
            .Register(UtilityActionCatalog.FollowTarget(
                "follow",
                settings.RangedIdealRange,
                settings.RangedStopDistance,
                settings.ChaseRange,
                weight: 3f))
            .Register(UtilityActionCatalog.AcquireTarget(
                "acquire",
                settings.AggroRadius,
                weight: 2f))
            .Register(UtilityActionCatalog.ReturnToSpawn(
                "return",
                settings.ReturnToSpawnTolerance,
                weight: 1f))
            .Register(UtilityActionCatalog.Patrol(
                "patrol-route",
                settings.PatrolRadius,
                settings.PatrolWaypointCount,
                settings.PatrolStopDistance,
                settings.PatrolDwellTime,
                weight: 1.2f))
            .Register(UtilityActionCatalog.Idle("idle", settings.IdleAnimation, weight: 0.2f));
    }

    public static UtilityAgent CreateCaster(Skill fireball, Skill frostbolt, Skill basicAttack, UtilityAgentSettings? settings = null)
    {
        settings ??= UtilityAgentSettings.Default;

        return new UtilityAgent("caster")
            .Register(UtilityActionCatalog.UseSkill(
                "fireball",
                fireball,
                settings.CasterOptimalRange,
                settings.CasterMaxRange,
                settings.FireballCooldown,
                weight: 7f))
            .Register(UtilityActionCatalog.UseSkill(
                "frostbolt",
                frostbolt,
                settings.CasterOptimalRange,
                settings.CasterMaxRange,
                settings.FrostboltCooldown,
                weight: 6f))
            .Register(UtilityActionCatalog.UseSkill(
                "basic-attack",
                basicAttack,
                settings.RangedIdealRange,
                settings.RangedMaxRange,
                settings.BasicAttackCooldown,
                weight: 4f))
            .Register(UtilityActionCatalog.FollowTarget(
                "kite",
                settings.CasterKiteRange,
                settings.RangedStopDistance,
                settings.ChaseRange,
                weight: 3f))
            .Register(UtilityActionCatalog.AcquireTarget(
                "acquire",
                settings.AggroRadius,
                weight: 2f))
            .Register(UtilityActionCatalog.ReturnToSpawn(
                "return",
                settings.ReturnToSpawnTolerance,
                weight: 1f))
            .Register(UtilityActionCatalog.Patrol(
                "patrol-route",
                settings.PatrolRadius,
                settings.PatrolWaypointCount,
                settings.PatrolStopDistance,
                settings.PatrolDwellTime,
                weight: 1.1f))
            .Register(UtilityActionCatalog.Idle("idle", settings.IdleAnimation, weight: 0.2f));
    }

    public static UtilityAgent CreateBoss(Skill ultimate, Skill powerAttack, Skill basicAttack, UtilityAgentSettings? settings = null)
    {
        settings ??= UtilityAgentSettings.Default;

        return new UtilityAgent("boss")
            .Register(UtilityActionCatalog.UseSkill(
                "ultimate",
                ultimate,
                settings.BossUltimateRange,
                settings.BossUltimateMaxRange,
                settings.UltimateCooldown,
                weight: 10f))
            .Register(UtilityActionCatalog.UseSkill(
                "power-attack",
                powerAttack,
                settings.BossPowerAttackRange,
                settings.BossPowerAttackMaxRange,
                settings.PowerAttackCooldown,
                weight: 8f))
            .Register(UtilityActionCatalog.UseSkill(
                "basic-attack",
                basicAttack,
                settings.MeleeRange,
                settings.MeleeMaxRange,
                settings.BasicAttackCooldown,
                weight: 6f))
            .Register(UtilityActionCatalog.FollowTarget(
                "chase",
                settings.MeleeRange,
                settings.MeleeStopDistance,
                settings.BossChaseRange,
                weight: 5f))
            .Register(UtilityActionCatalog.AcquireTarget(
                "acquire",
                settings.BossAggroRadius,
                weight: 3f))
            .Register(UtilityActionCatalog.ReturnToSpawn(
                "reset",
                settings.ReturnToSpawnTolerance,
                weight: 1f))
            .Register(UtilityActionCatalog.Patrol(
                "patrol-route",
                settings.PatrolRadius,
                settings.PatrolWaypointCount,
                settings.PatrolStopDistance,
                settings.PatrolDwellTime,
                weight: 1.5f))
            .Register(UtilityActionCatalog.Idle("idle", settings.BossIdleAnimation, weight: 0.2f));
    }

    public static UtilityAgent CreateFriendlyMerchant(
        DialogueComponent? dialogue,
        MerchantComponent? merchant,
        QuestGiverComponent? questGiver,
        UtilityAgentSettings? settings = null)
    {
        settings ??= UtilityAgentSettings.Default;
        var interactionRange = settings.InteractionRange;
        var scriptName = string.IsNullOrWhiteSpace(dialogue?.DialogueScript)
            ? settings.DialogueScript
            : dialogue!.DialogueScript;
    var dialogueParameters = ToDialogueParameters(dialogue);
        var questArray = questGiver?.AvailableQuests?.ToArray() ?? settings.DefaultQuestIds.ToArray();

        var agent = new UtilityAgent("friendly-merchant")
            .Register(UtilityActionCatalog.AcquireTarget("await-player", interactionRange, weight: 2f))
            .Register(UtilityActionCatalog.Dialogue("greet", scriptName, interactionRange, dialogueParameters, weight: 3f))
            .Register(UtilityActionCatalog.React("acknowledge", "wave", weight: 1.2f));

        if (merchant != null)
        {
            agent.Register(UtilityActionCatalog.OpenMerchant("open-merchant", interactionRange, weight: 4f));
        }

        if (questArray.Length > 0)
        {
            agent.Register(UtilityActionCatalog.OfferQuest("offer-quest", questArray, interactionRange, weight: 2f));
        }

        return agent.Register(UtilityActionCatalog.Idle("idle", settings.IdleAnimation, weight: 0.5f));
    }

    public static UtilityAgent CreateFriendlyGreeter(
        DialogueComponent? dialogue,
        QuestGiverComponent? questGiver,
        UtilityAgentSettings? settings = null)
    {
        settings ??= UtilityAgentSettings.Default;
        var interactionRange = settings.InteractionRange;
        var scriptName = string.IsNullOrWhiteSpace(dialogue?.DialogueScript)
            ? settings.DialogueScript
            : dialogue!.DialogueScript;
    var dialogueParameters = ToDialogueParameters(dialogue);
        var questArray = questGiver?.AvailableQuests?.ToArray() ?? Array.Empty<Guid>();

        var agent = new UtilityAgent("friendly")
            .Register(UtilityActionCatalog.AcquireTarget("await-player", interactionRange, weight: 2f))
            .Register(UtilityActionCatalog.Dialogue("greet", scriptName, interactionRange, dialogueParameters, weight: 3.5f))
            .Register(UtilityActionCatalog.React("respond", "nod", weight: 1.5f));

        if (questArray.Length > 0)
        {
            agent.Register(UtilityActionCatalog.OfferQuest("offer-quest", questArray, interactionRange, weight: 2.5f));
        }

        return agent.Register(UtilityActionCatalog.Idle("idle", settings.IdleAnimation, weight: 0.6f));
    }

    public static UtilityAgent? GetByName(
        string scriptName,
        IDictionary<string, Skill> skills,
        UtilityAgentSettings? settings = null,
        DialogueComponent? dialogue = null,
        MerchantComponent? merchant = null,
        QuestGiverComponent? questGiver = null)
    {
        settings ??= UtilityAgentSettings.Default;
        var lookup = scriptName.ToLowerInvariant();

        return lookup switch
        {
            "aggressive-melee" or "hostile-melee" => CreateAggressiveMelee(RequireSkill(skills, "basic-attack"), settings),
            "defensive-healer" => CreateDefensiveHealer(RequireSkill(skills, "heal"), RequireSkill(skills, "basic-attack"), settings),
            "caster" or "hostile-caster" => CreateCaster(
                RequireSkill(skills, "fireball"),
                RequireSkill(skills, "frostbolt"),
                RequireSkill(skills, "basic-attack"),
                settings),
            "boss" => CreateBoss(
                RequireSkill(skills, "ultimate"),
                RequireSkill(skills, "power-attack"),
                RequireSkill(skills, "basic-attack"),
                settings),
            "friendly-merchant" => merchant != null
                ? CreateFriendlyMerchant(dialogue, merchant, questGiver, settings)
                : CreateFriendlyGreeter(dialogue, questGiver, settings),
            "friendly-questgiver" => CreateFriendlyMerchant(dialogue, merchant, questGiver, settings),
            "friendly-greeter" or "friendly" => CreateFriendlyGreeter(dialogue, questGiver, settings),
            _ => null
        };
    }

    private static Skill RequireSkill(IDictionary<string, Skill> skills, string key)
    {
        if (!skills.TryGetValue(key, out var skill))
        {
            throw new ArgumentException($"Skill '{key}' is required for this behavior.", nameof(skills));
        }

        return skill;
    }

    private static IDictionary<string, object?> ToDialogueParameters(DialogueComponent? dialogue)
    {
        if (dialogue?.ScriptParameters is { Count: > 0 })
        {
            return dialogue.ScriptParameters.ToDictionary(
                kvp => kvp.Key,
                kvp => (object?)kvp.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record UtilityAgentSettings(
    float AggroRadius,
    float MeleeRange,
    float MeleeStopDistance,
    float MeleeMaxRange,
    float ChaseRange,
    TimeSpan? BasicAttackCooldown,
    float RangedIdealRange,
    float RangedStopDistance,
    float RangedMaxRange,
    TimeSpan? FireballCooldown,
    TimeSpan? FrostboltCooldown,
    TimeSpan? HealCooldown,
    float SelfHealOptimalRange,
    float SelfHealMaxRange,
    float CasterOptimalRange,
    float CasterMaxRange,
    float CasterKiteRange,
    float BossUltimateRange,
    float BossUltimateMaxRange,
    TimeSpan? UltimateCooldown,
    float BossPowerAttackRange,
    float BossPowerAttackMaxRange,
    TimeSpan? PowerAttackCooldown,
    float BossChaseRange,
    float BossAggroRadius,
    float ReturnToSpawnTolerance,
    float PatrolRadius,
    int PatrolWaypointCount,
    float PatrolStopDistance,
    TimeSpan PatrolDwellTime,
    string? IdleAnimation,
    string BossIdleAnimation,
    string DialogueScript,
    float InteractionRange,
    IReadOnlyList<Guid> DefaultQuestIds)
{
    public static UtilityAgentSettings Default { get; } = new(
        AggroRadius: 25f,
        MeleeRange: 2.5f,
        MeleeStopDistance: 1.5f,
        MeleeMaxRange: 4f,
        ChaseRange: 35f,
        BasicAttackCooldown: TimeSpan.FromSeconds(2.5),
        RangedIdealRange: 8f,
        RangedStopDistance: 5f,
        RangedMaxRange: 15f,
        FireballCooldown: TimeSpan.FromSeconds(6),
        FrostboltCooldown: TimeSpan.FromSeconds(3),
        HealCooldown: TimeSpan.FromSeconds(8),
        SelfHealOptimalRange: 0f,
        SelfHealMaxRange: 1f,
        CasterOptimalRange: 18f,
        CasterMaxRange: 30f,
        CasterKiteRange: 20f,
        BossUltimateRange: 12f,
        BossUltimateMaxRange: 18f,
        UltimateCooldown: TimeSpan.FromSeconds(15),
        BossPowerAttackRange: 6f,
        BossPowerAttackMaxRange: 12f,
        PowerAttackCooldown: TimeSpan.FromSeconds(8),
        BossChaseRange: 45f,
        BossAggroRadius: 40f,
        ReturnToSpawnTolerance: 1f,
    PatrolRadius: 8f,
    PatrolWaypointCount: 6,
    PatrolStopDistance: 0.75f,
    PatrolDwellTime: TimeSpan.FromSeconds(2),
        IdleAnimation: "idle",
        BossIdleAnimation: "taunt",
        DialogueScript: "merchant-greeting",
        InteractionRange: 3f,
        DefaultQuestIds: Array.Empty<Guid>());
}
