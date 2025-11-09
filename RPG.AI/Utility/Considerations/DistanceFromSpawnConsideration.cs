using System;
using System.Numerics;
using RPG.AI.Core;

namespace RPG.AI.Utility.Considerations;

public sealed class DistanceFromSpawnConsideration : IUtilityConsideration
{
    private readonly float _threshold;

    public DistanceFromSpawnConsideration(string name, float threshold)
    {
        Name = name;
        _threshold = threshold;
    }

    public string Name { get; }

    public float Evaluate(AiContext context)
    {
        var spawn = context.Self?.SpawnLocation;
        var current = context.Self?.CurrentLocation;
        if (spawn?.Position is not Vector3 spawnPos || current?.Position is not Vector3 currentPos)
        {
            return 0f;
        }

        var distance = Vector3.Distance(spawnPos, currentPos);
        if (distance <= _threshold)
        {
            return 0f;
        }

        return Math.Clamp(distance / (_threshold * 2f), 0f, 1f);
    }
}
