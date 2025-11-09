using System;
using System.Numerics;
using RPG.AI.Core;

namespace RPG.AI.Utility.Considerations;

/// <summary>
///     Scores based on whether players are within the specified radius.
/// </summary>
public sealed class NearbyPlayersConsideration : IUtilityConsideration
{
    private readonly float _radius;

    public NearbyPlayersConsideration(string name, float radius)
    {
        Name = name;
        _radius = radius;
    }

    public string Name { get; }

    public float Evaluate(AiContext context)
    {
        if (context.NearbyPlayers.Count == 0)
        {
            return 0f;
        }

        var npcPosition = context.Self?.CurrentLocation?.Position;
        if (npcPosition == null)
        {
            return 0f;
        }

        var radiusSquared = _radius * _radius;
        foreach (var player in context.NearbyPlayers)
        {
            var playerPosition = player.CurrentLocation?.Position;
            if (playerPosition == null)
            {
                continue;
            }

            var distanceSquared = Vector3.DistanceSquared(npcPosition.Value, playerPosition.Value);
            if (distanceSquared <= radiusSquared)
            {
                return 1f;
            }
        }

        return 0f;
    }
}
