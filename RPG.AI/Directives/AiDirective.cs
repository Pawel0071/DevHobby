using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Domain.Models;
using RPG.Domain.Models.Skills;

namespace RPG.AI.Directives;

/// <summary>
///     High-level instruction emitted by the AI system. Game systems interpret directives to
///     drive movement, conversations, quests, and combat.
/// </summary>
public sealed record AiDirective(
    AiDirectiveType Type,
    Guid? TargetId = null,
    Location? Destination = null,
    float? DesiredRange = null,
    float? StopDistance = null,
    string? ScriptName = null,
    IReadOnlyDictionary<string, object?>? Metadata = null)
{
    public static AiDirective Idle(string? animation = null)
    {
        return new AiDirective(
            AiDirectiveType.Idle,
            Metadata: animation is null
                ? null
                : new Dictionary<string, object?> { ["animation"] = animation });
    }

    public static AiDirective MoveTo(Location destination, float stopDistance = 1f)
    {
        return new AiDirective(
            AiDirectiveType.MoveToLocation,
            Destination: CloneLocation(destination),
            StopDistance: stopDistance);
    }

    public static AiDirective FollowTarget(Guid targetId, float desiredRange, float stopDistance, float? maxRange = null)
    {
        var metadata = maxRange.HasValue
            ? new Dictionary<string, object?> { ["maxRange"] = maxRange.Value }
            : null;

        return new AiDirective(
            AiDirectiveType.FollowTarget,
            TargetId: targetId,
            DesiredRange: desiredRange,
            StopDistance: stopDistance,
            Metadata: metadata);
    }

    public static AiDirective StopMovement()
    {
        return new AiDirective(AiDirectiveType.StopMovement);
    }

    public static AiDirective BeginDialogue(Guid targetId, string scriptName, IReadOnlyDictionary<string, object?>? parameters)
    {
        return new AiDirective(
            AiDirectiveType.BeginDialogue,
            TargetId: targetId,
            ScriptName: scriptName,
            Metadata: parameters == null ? null : new Dictionary<string, object?>(parameters));
    }

    public static AiDirective OpenShop(Guid npcId, Guid? targetId = null)
    {
        return new AiDirective(
            AiDirectiveType.OpenShop,
            TargetId: targetId,
            Metadata: new Dictionary<string, object?> { ["merchantId"] = npcId });
    }

    public static AiDirective OfferQuest(Guid npcId, IEnumerable<Guid> questIds, Guid? targetId = null)
    {
        var quests = questIds switch
        {
            IReadOnlyCollection<Guid> readOnly => readOnly,
            ICollection<Guid> collection => new List<Guid>(collection),
            _ => questIds.ToArray()
        };

        return new AiDirective(
            AiDirectiveType.OfferQuest,
            TargetId: targetId,
            Metadata: new Dictionary<string, object?>
            {
                ["questGiverId"] = npcId,
                ["quests"] = quests
            });
    }

    public static AiDirective Reaction(string reactionType, Guid? targetId = null)
    {
        return new AiDirective(
            AiDirectiveType.Reaction,
            TargetId: targetId,
            Metadata: new Dictionary<string, object?> { ["reaction"] = reactionType });
    }

    public static AiDirective UseSkill(Skill skill, Guid? targetId = null, IReadOnlyDictionary<string, object?>? payload = null)
    {
        if (skill == null)
        {
            throw new ArgumentNullException(nameof(skill));
        }

        var metadata = new Dictionary<string, object?>
        {
            ["skillId"] = skill.Id,
            ["skillName"] = skill.Name
        };

        if (payload != null)
        {
            foreach (var pair in payload)
            {
                metadata[pair.Key] = pair.Value;
            }
        }

        return new AiDirective(
            AiDirectiveType.UseSkill,
            TargetId: targetId,
            Metadata: metadata);
    }

    private static Location CloneLocation(Location location)
    {
        if (location == null)
        {
            return new Location();
        }

        return new Location
        {
            Position = location.Position,
            Rotation = location.Rotation,
            MapId = location.MapId,
            ZoneName = location.ZoneName,
            WorldId = location.WorldId
        };
    }
}

public enum AiDirectiveType
{
    Idle,
    MoveToLocation,
    FollowTarget,
    StopMovement,
    BeginDialogue,
    OpenShop,
    OfferQuest,
    Reaction,
    UseSkill
}
