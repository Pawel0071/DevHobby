using System.Collections;
using RPG.GameServer.Application.Movement;
using RPG.GameServer.Domain.Entities.Common;
using RPG.GameServer.Interfaces;

namespace RPG.SessionKeeper.Application.Movement;

public class AggroSystem
{
    private readonly IMonsterRepository _monsterRepo;

    public AggroSystem(IEventBus eventBus, IMonsterRepository monsterRepo)
    {
        _monsterRepo = monsterRepo;
        eventBus.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
    }

    private void OnPlayerMoved(PlayerMovedEvent evt)
    {
        var nearbyMonsters = _monsterRepo.GetMonstersNear(evt.NewPosition, radius: 20f);

        foreach (var monster in nearbyMonsters)
        {
            if (monster.IsPlayerInAggroRange(evt.NewPosition))
            {
                monster.Behavior.TriggerAggro(evt.PlayerId);
            }
        }
    }
}
