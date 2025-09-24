using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPG.Core.Interfaces;

public interface IAiBehavior
{
    void TriggerAggro(string evtPlayerId);
    void Patrol();
    void Chase(PlayerCharacter target);
    void Flee(PlayerCharacter threat);
    void UseSkill(string skillName, PlayerCharacter target);
    void Die();
}