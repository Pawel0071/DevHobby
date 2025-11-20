// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Core/Services/NpcServices/BehaviorRegistry.cs
using System.Collections.Concurrent;
using RPG.AI.Utility;
using RPG.AI.Utility.Actions;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Npcs.NpcComponents;
using RPG.Domain.Models.Skills;
using RPG.Core.Interfaces.NpcServices;

namespace RPG.Core.Services.NpcServices;

public sealed class BehaviorRegistry : IBehaviorRegistry
{
    private readonly ConcurrentDictionary<Guid, UtilityAgent> _cache = new();

    public UtilityAgent GetOrCreateAgent(Npc npc)
    {
        return _cache.GetOrAdd(npc.Id, _ => CreateAgent(npc));
    }

    public void Invalidate(Npc npc)
    {
        _cache.TryRemove(npc.Id, out _);
    }

    private static UtilityAgent CreateAgent(Npc npc)
    {
        var combat = npc.Components.OfType<CombatComponent>().FirstOrDefault();
        var dialogue = npc.Components.OfType<DialogueComponent>().FirstOrDefault();
        var merchant = npc.Components.OfType<MerchantComponent>().FirstOrDefault();
        var questGiver = npc.Components.OfType<QuestGiverComponent>().FirstOrDefault();
        var aiComponent = npc.Components.OfType<AiComponent>().FirstOrDefault();

        UtilityAgent agent;
        // Combat-centric
        if (combat != null)
        {
            var skills = npc.Skills.Keys.ToArray();
            Skill? primary = skills.FirstOrDefault();

            // Boss
            if (npc.Tags.Contains("boss"))
            {
                var ultimate = skills.Skip(0).FirstOrDefault() ?? primary ?? Skill.Create("ultimate");
                var power = skills.Skip(1).FirstOrDefault() ?? primary ?? Skill.Create("power-attack");
                var basic = skills.Skip(2).FirstOrDefault() ?? primary ?? Skill.Create("basic-attack");
                agent = UtilityAgentFactory.CreateBoss(ultimate, power, basic);
            }
            // Caster-like (heurystyka po tagach)
            else if (npc.Tags.Any(t => t.Contains("caster", StringComparison.OrdinalIgnoreCase)))
            {
                var fireball = skills.FirstOrDefault() ?? Skill.Create("fireball");
                var frostbolt = skills.Skip(1).FirstOrDefault() ?? fireball;
                var basic = skills.Skip(2).FirstOrDefault() ?? fireball;
                agent = UtilityAgentFactory.CreateCaster(fireball, frostbolt, basic);
            }
            // Default aggressive melee
            else if (primary != null)
            {
                agent = UtilityAgentFactory.CreateAggressiveMelee(primary);
            }
            else
            {
                agent = new UtilityAgent("combat-fallback").Register(UtilityActionCatalog.Idle("idle"));
            }
        }
        else if (merchant != null || dialogue != null || questGiver != null)
        {
            agent = UtilityAgentFactory.CreateFriendlyMerchant(dialogue, merchant, questGiver);
        }
        else if (dialogue != null || questGiver != null)
        {
            agent = UtilityAgentFactory.CreateFriendlyGreeter(dialogue, questGiver);
        }
        else
        {
            agent = new UtilityAgent("idle-only").Register(UtilityActionCatalog.Idle("idle"));
        }

        // Dynamic patrol injection based on AiComponent (overrides default settings if present)
        if (aiComponent?.Patrol is { } patrolCfg)
        {
            // Unikalny identyfikator akcji patrolu bazujący na NPC
            var actionKey = $"patrol:{npc.Id:N}";
            agent.Register(UtilityActionCatalog.Patrol(
                actionKey,
                radius: patrolCfg.Radius,
                waypointCount: Math.Max(1, patrolCfg.WaypointCount),
                stopDistance: Math.Max(0.1f, patrolCfg.StopDistance),
                dwellTime: TimeSpan.FromSeconds(Math.Max(0, patrolCfg.DwellTimeSeconds))));
        }

        return agent;
    }
}
