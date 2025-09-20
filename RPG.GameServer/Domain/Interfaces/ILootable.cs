using RPG.GameServer.Domain.Types;

namespace RPG.GameServer.Interfaces;

public interface ILootable
{
    List<LootDrop> LootTable { get; }
    List<Item> DropLoot();
}