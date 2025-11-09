using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using RPG.AI.Core;
using RPG.AI.Directives;
using RPG.AI.Utility.Considerations;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Skills;

namespace RPG.AI.Utility.Actions;

public static class UtilityActionCatalog
{
    private const string PatrolRouteKey = "patrol-route";
    private const string PatrolIndexKey = "patrol-index";
    private const string PatrolLastReachedKey = "patrol-last-reached";

    private static readonly ThreadLocal<Random> Random = new(() => new Random());

    public static UtilityActionDefinition AcquireTarget(string name, float range, float weight = 1f)
    {
        return new UtilityActionDefinition(
            name,
            context =>
            {
                var npcPosition = context.Self?.CurrentLocation?.Position ?? Vector3.Zero;
                Character? chosen = null;
                var chosenDistance = float.PositiveInfinity;

                if (context.ThreatTable.Count > 0)
                {
                    foreach (var threat in context.ThreatTable.Values.OrderByDescending(t => t.Score))
                    {
                        var candidate = context.NearbyPlayers.FirstOrDefault(p => p.Id == threat.CharacterId);
                        if (candidate == null)
                        {
                            continue;
                        }

                        var position = candidate.CurrentLocation?.Position ?? Vector3.Zero;
                        var distance = Vector3.Distance(npcPosition, position);
                        if (distance <= range)
                        {
                            chosen = candidate;
                            chosenDistance = distance;
                            break;
                        }
                    }
                }

                if (chosen == null)
                {
                    foreach (var player in context.NearbyPlayers)
                    {
                        var position = player.CurrentLocation?.Position ?? Vector3.Zero;
                        var distance = Vector3.Distance(npcPosition, position);
                        if (distance <= range && distance < chosenDistance)
                        {
                            chosen = player;
                            chosenDistance = distance;
                        }
                    }
                }

                if (chosen != null)
                {
                    context.Target = chosen;
                    context.SetBlackboardValue("targetId", chosen.Id);
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

    public static UtilityActionDefinition Patrol(string name, float radius, int waypointCount, float stopDistance, TimeSpan dwellTime, float weight = 1f)
    {
        waypointCount = Math.Max(1, waypointCount);

        return new UtilityActionDefinition(
            name,
            context =>
            {
                var self = context.Self;
                var spawn = self?.SpawnLocation;
                if (self == null || spawn == null)
                {
                    return Array.Empty<AiDirective>();
                }

                var routeKey = $"{PatrolRouteKey}:{self.Id}";
                if (!context.TryGetBlackboardValue(routeKey, out List<Location>? route) || route is null || route.Count == 0)
                {
                    route = GeneratePatrolRoute(spawn, radius, waypointCount);
                    context.SetBlackboardValue(routeKey, route);
                    context.SetBlackboardValue(IndexKey(name, self.Id), 0);
                    context.SetBlackboardValue(LastReachedKey(name, self.Id), DateTime.UtcNow);
                }

                var indexKey = IndexKey(name, self.Id);
                if (!context.TryGetBlackboardValue<int>(indexKey, out var index) || index < 0 || index >= route.Count)
                {
                    index = 0;
                    context.SetBlackboardValue(indexKey, index);
                }

                var destination = route[index];
                var distance = context.CalculateDistanceTo(destination);

                if (distance <= stopDistance)
                {
                    context.SetBlackboardValue(LastReachedKey(name, self.Id), DateTime.UtcNow);
                    index = (index + 1) % route.Count;
                    context.SetBlackboardValue(indexKey, index);
                    destination = route[index];
                }
                else if (context.TryGetBlackboardValue(LastReachedKey(name, self.Id), out DateTime lastReached) &&
                         DateTime.UtcNow - lastReached < dwellTime)
                {
                    return new[] { AiDirective.Idle("patrol-wait") };
                }

                return new[] { AiDirective.MoveTo(destination, stopDistance) };
            },
            new IUtilityConsideration[]
            {
                new NoTargetConsideration()
            },
            weight,
            context => !context.IsInCombat);
    }

    public static UtilityActionDefinition Idle(string name, string? animation = null, float weight = 0.1f)
    {
        return new UtilityActionDefinition(
            name,
            context => new[] { AiDirective.Idle(animation) },
            Array.Empty<IUtilityConsideration>(),
            weight);
    }

    public static UtilityActionDefinition Dialogue(string name, string scriptName, float interactionRange, IDictionary<string, object?>? parameters = null, float weight = 1f)
    {
        return new UtilityActionDefinition(
            name,
            context =>
            {
                if (context.Target == null)
                {
                    return Array.Empty<AiDirective>();
                }

                var payload = parameters != null
                    ? new Dictionary<string, object?>(parameters)
                    : new Dictionary<string, object?>();

                payload["script"] = scriptName;
                payload["initiatedBy"] = "utility-ai";

                return new[] { AiDirective.BeginDialogue(context.Target.Id, scriptName, payload) };
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
                if (context.Target == null || context.Self == null)
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

    public static UtilityActionDefinition React(string name, string reactionType, float weight = 0.5f)
    {
        return new UtilityActionDefinition(
            name,
            context =>
            {
                if (context.Target == null)
                {
                    return Array.Empty<AiDirective>();
                }

                return new[] { AiDirective.Reaction(reactionType, context.Target.Id) };
            },
            new IUtilityConsideration[]
            {
                new HasTargetConsideration()
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
                if (context.Target == null || context.Self == null)
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

    private static List<Location> GeneratePatrolRoute(Location spawn, float radius, int waypointCount)
    {
        var route = new List<Location>(waypointCount);
        var basePosition = spawn.Position;
        var worldId = spawn.WorldId ?? Guid.Empty;

        for (var i = 0; i < waypointCount; i++)
        {
            var angle = (360f / waypointCount) * i + NextFloat() * 45f;
            var radians = MathF.PI / 180f * angle;
            var distance = radius * (0.5f + NextFloat() * 0.5f);

            var offset = new Vector3(
                distance * MathF.Cos(radians),
                0f,
                distance * MathF.Sin(radians));

            var waypoint = Location.Create(basePosition + offset, worldId, spawn.MapId, spawn.ZoneName);
            waypoint.Rotation = angle % 360f;
            route.Add(waypoint);
        }

        return route;
    }

    private static float NextFloat()
    {
        var rng = Random.Value ?? throw new InvalidOperationException("Random generator not initialized");
        return (float)rng.NextDouble();
    }

    private static string IndexKey(string name, Guid npcId) => $"{name}:{npcId}:{PatrolIndexKey}";

    private static string LastReachedKey(string name, Guid npcId) => $"{name}:{npcId}:{PatrolLastReachedKey}";
}
