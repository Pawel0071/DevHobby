using RPG.GameServer;
using RPG.GameServer.Domain.Entities.Common;
using RPG.SessionKeeper;

namespace RPG.GameServer.Application.Movement;

public class PlayerMovedEvent
{
    public string PlayerId { get; set; }
    public Location NewPosition { get; set; }
}