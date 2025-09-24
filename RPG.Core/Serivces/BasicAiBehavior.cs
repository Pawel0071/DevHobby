using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Entities.Common;
using System;

namespace RPG.Core.Interfaces;

public class BasicAiBehavior : IAiBehavior
{
    private readonly MonsterCharacter<DefaultMovementPattern> _monster;
    private readonly PlayerCharacter _player;

    public BasicAiBehavior(MonsterCharacter<DefaultMovementPattern> monster, PlayerCharacter player)
    {
        _monster = monster;
        _player = player;
    }

    public void TriggerAggro(string evtPlayerId)
    {
        if (_player.Id == evtPlayerId && IsWithinAggroRange(_monster, _player))
        {
            // Logic for monster chasing the player
            _monster.MovementPattern.MoveTowards(_monster, _player.Position);
        }
    }

    private bool IsWithinAggroRange(MonsterCharacter<DefaultMovementPattern> monster, PlayerCharacter player)
    {
        var distance = CalculateDistance(monster.Position, player.Position);
        return distance <= monster.AggroRange;
    }

    private double CalculateDistance(Location a, Location b)
    {
        return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));
    }

    // Implemented IAiBehavior methods
    public void Patrol()
    {
        Console.WriteLine($"{_monster.Name} is patrolling.");
    }

    public void Chase(PlayerCharacter target)
    {
        Console.WriteLine($"{_monster.Name} is chasing {target.Name}.");
        _monster.MovementPattern.MoveTowards(_monster, target.Position);
    }

    public void Flee(PlayerCharacter threat)
    {
        Console.WriteLine($"{_monster.Name} is fleeing from {threat.Name}.");
        // Logic to move away from the threat
    }

    public void UseSkill(string skillName, PlayerCharacter target)
    {
        Console.WriteLine($"{_monster.Name} uses {skillName} on {target.Name}.");
        // Logic to apply skill effects
    }

    public void Die()
    {
        Console.WriteLine($"{_monster.Name} has died.");
        // Logic for death behavior
    }

    public void Move<TMovementPattern>() where TMovementPattern : IMovementPattern, new()
    {
        throw new NotImplementedException();
    }

    public void Attack<TMovementPattern>(PlayerCharacter target) where TMovementPattern : IMovementPattern, new()
    {
        throw new NotImplementedException();
    }

    public void Idle<TMovementPattern>() where TMovementPattern : IMovementPattern, new()
    {
        throw new NotImplementedException();
    }
}