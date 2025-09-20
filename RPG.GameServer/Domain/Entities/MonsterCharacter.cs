using RPG.GameServer.Domain.Entities.Common;
using RPG.GameServer.Domain.Types;
using RPG.GameServer.Interfaces;

namespace RPG.GameServer.Domain.Entities;

public class MonsterCharacter<TMovementPattern> : BaseCharacter,  ILootable
{
    public MonsterType Type { get; set; }
    public int DamageMin { get; set; }
    public int DamageMax { get; set; }
    public float AttackSpeed { get; set; }
    public IAiBehavior Behavior { get; set; } = null!;
    public float AggroRange { get; set; }
    public TMovementPattern MovementPattern { get; set; } = default!;
    public List<LootDrop> LootTable { get; set; } = [];
    public List<Effect> Immunities { get; set; } = [];
    public List<Effect> Weaknesses { get; set; } = [];
    
    public List<Item> DropLoot()
    {
        var rng = new Random();
        return LootTable
            .Where(drop => rng.NextDouble() < drop.DropChance)
            .Select(drop => new Item { Id = drop.ItemId, Name = "Looted Item" })
            .ToList();
    }

    public bool IsPlayerInAggroRange(Location evtNewPosition)
    {
        throw new NotImplementedException();
    }
}
