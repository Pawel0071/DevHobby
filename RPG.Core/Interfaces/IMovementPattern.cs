using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Entities.Common;

namespace RPG.Core.Interfaces;

public interface IMovementPattern
{
    void Move<TMovementPattern>(MonsterCharacter<TMovementPattern> monster) where TMovementPattern : IMovementPattern, new();

    void Attack<TMovementPattern>(MonsterCharacter<TMovementPattern> monster, PlayerCharacter target) where TMovementPattern : IMovementPattern, new();

    void Idle<TMovementPattern>(MonsterCharacter<TMovementPattern> monster) where TMovementPattern : IMovementPattern, new();

    void MoveTowards<TMovementPattern>(MonsterCharacter<TMovementPattern> monster, Location targetPosition) where TMovementPattern : IMovementPattern, new();
}