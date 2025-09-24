using RPG.Core.Domain.Entities.Common;

namespace RPG.Core.Interfaces;

public interface ILootable
{
    List<LootDrop> LootTable { get; }
    List<Item> DropLoot();
}