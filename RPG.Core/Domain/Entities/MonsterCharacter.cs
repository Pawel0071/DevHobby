using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Interfaces;

namespace RPG.Core.Domain.Entities;

public class MonsterCharacter<TMovementPattern> : BaseCharacter, ILootable where TMovementPattern : IMovementPattern, new()
{
    public MonsterType Type { get; set; }
    public int DamageMin { get; set; }
    public int DamageMax { get; set; }
    public float AttackSpeed { get; set; }
    public IAiBehavior Behavior { get; set; } = null!;
    public float AggroRange { get; set; }
    public TMovementPattern MovementPattern { get; set; } = new TMovementPattern();
    public List<LootDrop> LootTable { get; set; } = new();
    public List<Effect> Immunities { get; set; } = new();
    public List<Effect> Weaknesses { get; set; } = new();

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

    List<Item> ILootable.DropLoot()
    {
        throw new NotImplementedException();
    }
}

