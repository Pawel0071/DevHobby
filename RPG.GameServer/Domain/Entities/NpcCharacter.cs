using RPG.GameServer.Domain.Types;
using RPG.GameServer.Interfaces;

namespace RPG.GameServer.Domain.Entities;

public class NpcCharacter : BaseCharacter, IInteractive
{
    public NpcType Type { get; set; }
    public bool CanTrade { get; set; }
    public bool CanHeal { get; set; }
    public bool CanGiveQuest { get; set; }
    public IAiBehavior Behavior { get; set; } = null!;
    public List<Item> TradeInventory { get; set; } = [];
    public string DialogueScriptId { get; set; }
    public string InteractionType { get; }
    
    public string Interact(string initiatorId)
    {
        throw new NotImplementedException();
    }
}