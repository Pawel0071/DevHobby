using RPG.Core.Domain.Entities.Common;
using RPG.Core.Interfaces;

namespace RPG.Core.Domain.Entities.Common;

public class DefaultMovementPattern : IMovementPattern
{
    // Updated class to implement generic methods
    public void Move<TMovementPattern>(MonsterCharacter<TMovementPattern> monster) where TMovementPattern : IMovementPattern, new()
    {
        // Basic movement logic for the monster
        monster.Position.X += 1;
        monster.Position.Y += 1;
    }

    public void Attack<TMovementPattern>(MonsterCharacter<TMovementPattern> monster, PlayerCharacter target) where TMovementPattern : IMovementPattern, new()
    {
        // Basic attack logic
        int damage = new Random().Next(monster.DamageMin, monster.DamageMax);
        target.CurrentHealth -= damage;
    }

    public void Idle<TMovementPattern>(MonsterCharacter<TMovementPattern> monster) where TMovementPattern : IMovementPattern, new()
    {
        // Basic idle behavior
        Console.WriteLine($"{monster.Name} is idling.");
    }

    // Updated MoveTowards method to use generic MonsterCharacter
    public void MoveTowards<TMovementPattern>(MonsterCharacter<TMovementPattern> monster, Location targetPosition)
        where TMovementPattern : IMovementPattern, new()
    {
        Console.WriteLine($"{monster.Name} is moving towards {targetPosition}.");
        // Logic to update monster's position towards the target
        monster.Position.X += Math.Sign(targetPosition.X - monster.Position.X);
        monster.Position.Y += Math.Sign(targetPosition.Y - monster.Position.Y);
    }
}