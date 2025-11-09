using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using RPG.AI.Core;
using RPG.AI.Directives;
using RPG.AI.Utility.Considerations;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Skills;

namespace RPG.AI.Utility.Actions;

public static class UtilityActionCatalog
{
    public static UtilityActionDefinition AcquireTarget(string name, float range, float weight = 1f)
    {
        return new UtilityActionDefinition(
            name,
            context =>
            {
                var npcPosition = context.Self?.CurrentLocation?.Position ?? Vector3.Zero;
                Character? closest = null;
                var closestDistance = float.PositiveInfinity;

                foreach (var player in context.NearbyPlayers)
                {
                    var position = player.CurrentLocation?.Position ?? Vector3.Zero;
                    var distance = Vector3.Distance(npcPosition, position);
                    if (distance <= range && distance < closestDistance)
                    {
                        closest = player;
                        closestDistance = distance;
                    }
                }

                if (closest != null)
                {
                    context.Target = closest;
                }

                return Array.Empty<AiDirective>();
            },
            new IUtilityConsideration[]
            {
                new NoTargetConsideration(),
                new NearbyPlayersConsideration("players-nearby", range)
            },
            weight);
    }

    public static UtilityActionDefinition FollowTarget(string name, float desiredRange, float stopDistance, float chaseRange, float weight = 1.5f)
    {
        return new UtilityActionDefinition(
            name,
            context => new[]
            {
                context.Target is null
                    ? AiDirective.StopMovement()
                    : AiDirective.FollowTarget(context.Target.Id, desiredRange, stopDistance, chaseRange)
            },
            new IUtilityConsideration[]
            {
                new HasTargetConsideration(),
                new DistanceGreaterThanConsideration("distance-outside-range", desiredRange, chaseRange)
            },
            weight);
    }

    public static UtilityActionDefinition UseSkill(string name, Skill skill, float idealRange, float maxRange, TimeSpan? cooldown = null, float weight = 3f)
    {
        return new UtilityActionDefinition(
            name,
            context =>
            {
                if (context.Target is null)
                {
                    return Array.Empty<AiDirective>();
                }

                if (cooldown.HasValue)
                {
                    context.SkillCooldowns[skill.Id] = DateTime.UtcNow + cooldown.Value;
                }

                return new[] { AiDirective.UseSkill(skill, context.Target.Id) };
            },
            new IUtilityConsideration[]
            {
                new HasTargetConsideration(),
                new DistanceToTargetConsideration("ideal-range", idealRange, maxRange),
                cooldown.HasValue
                    ? new CooldownConsideration("cooldown", skill.Id, cooldown.Value)
                    : new AlwaysReadyConsideration()
            },
            weight);
    }

    public static UtilityActionDefinition ReturnToSpawn(string name, float stopDistance, float weight = 1f)
    {
        return new UtilityActionDefinition(
            name,
            context =>
            {
                var spawn = context.Self?.SpawnLocation;
                if (spawn == null)
                {
                    return Array.Empty<AiDirective>();
                }

                return new[] { AiDirective.MoveTo(spawn, stopDistance) };
            },
            new IUtilityConsideration[]
            {
                new NoTargetConsideration(),
                new DistanceFromSpawnConsideration("away-from-spawn", stopDistance)
            },
            weight);
    }

    public static UtilityActionDefinition Idle(string name, string? animation = null, float weight = 0.1f)
    {
        return new UtilityActionDefinition(
            name,
            context => new[] { AiDirective.Idle(animation) },
            Array.Empty<IUtilityConsideration>(),
            weight);
    }

    public static UtilityActionDefinition Dialogue(string name, string scriptName, float interactionRange, float weight = 1f)
    {
        return new UtilityActionDefinition(
            name,
            context =>
            {
                if (context.Target == null)
                {
                    return Array.Empty<AiDirective>();
                }

                var parameters = new Dictionary<string, object?> { ["script"] = scriptName };
                return new[] { AiDirective.BeginDialogue(context.Target.Id, scriptName, parameters) };
            },
            new IUtilityConsideration[]
            {
                new HasTargetConsideration(),
                new DistanceToTargetConsideration("in-dialogue-range", interactionRange, interactionRange * 1.5f)
            },
            weight);
    }

    public static UtilityActionDefinition OpenMerchant(string name, float interactionRange, float weight = 1f)
    {
        return new UtilityActionDefinition(
            name,
            context =>
            {
                if (context.Target == null)
                {
                    return Array.Empty<AiDirective>();
                }

                return new[] { AiDirective.OpenShop(context.Self.Id, context.Target.Id) };
            },
            new IUtilityConsideration[]
            {
                new HasTargetConsideration(),
                new DistanceToTargetConsideration("merchant-range", interactionRange, interactionRange * 1.5f)
            },
            weight);
    }

    public static UtilityActionDefinition OfferQuest(string name, IEnumerable<Guid> questIds, float interactionRange, float weight = 1f)
    {
        var quests = questIds.ToArray();

        return new UtilityActionDefinition(
            name,
            context =>
            {
                if (context.Target == null)
                {
                    return Array.Empty<AiDirective>();
                }

                return new[] { AiDirective.OfferQuest(context.Self.Id, quests, context.Target.Id) };
            },
            new IUtilityConsideration[]
            {
                new HasTargetConsideration(),
                new DistanceToTargetConsideration("quest-offer-range", interactionRange, interactionRange * 1.5f)
            },
            weight);
    }

    private sealed class AlwaysReadyConsideration : IUtilityConsideration
    {
        public string Name => "always-ready";

        public float Evaluate(AiContext context) => 1f;
    }
}
