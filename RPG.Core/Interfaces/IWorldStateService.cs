using System;
using RPG.Domain.Entities;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.Npcs;

namespace RPG.Core.Interfaces;

public interface IWorldStateService
{
    void UpsertCharacter(WorldState world, Character character);
    void RemoveCharacter(WorldState world, Guid characterId);
    void UpsertNpc(WorldState world, Npc npc);
    void UpsertMapObject(WorldState world, MapObject mapObject);
    void Touch(WorldState world, DateTime timestamp);
    WorldState Clone(WorldState world);
}
