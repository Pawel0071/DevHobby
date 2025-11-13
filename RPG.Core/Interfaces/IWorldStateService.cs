using System;
using System.Threading;
using System.Threading.Tasks;
using RPG.Domain.Models;
using RPG.Domain.Models.MapObjects;
using RPG.Domain.Models.Npcs;

namespace RPG.Core.Interfaces;

public interface IWorldStateService
{
    void UpsertCharacter(WorldState world, Character character);
    void RemoveCharacter(WorldState world, Guid characterId);
    void UpsertNpc(WorldState world, Npc npc);
    void UpsertMapObject(WorldState world, MapObject mapObject);
    void Touch(WorldState world, DateTime timestamp);
    WorldState Clone(WorldState world);
    Task<Location> DetermineSpawnLocationAsync(WorldState world, Character character, string? spawnType = null, bool useExistingLocation = true, CancellationToken cancellationToken = default);
}
