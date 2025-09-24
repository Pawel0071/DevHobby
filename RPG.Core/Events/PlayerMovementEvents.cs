using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Entities.Common;
using System;
using System.Linq;

namespace RPG.Core.Interfaces;

public static class PlayerMovementEvents
{
    public static event Action<PlayerCharacter> OnPlayerMoved = delegate { };

    public static void PlayerMoved(PlayerCharacter player, Location newPosition, WorldMap worldMap)
    {
        player.Position = newPosition;
        OnPlayerMoved?.Invoke(player);

        // Check nearby monsters
        foreach (var region in worldMap.Regions)
        {
            foreach (var location in region.Locations)
            {
                var nearbyMonsters = location.Monsters
                    .Where(monster => CalculateDistance(monster.Position, player.Position) <= monster.AggroRange)
                    .ToList();

                foreach (var monster in nearbyMonsters)
                {
                    monster.Behavior.TriggerAggro(player.Id);
                }
            }
        }
    }

    private static double CalculateDistance(Location a, Location b)
    {
        return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));
    }
}