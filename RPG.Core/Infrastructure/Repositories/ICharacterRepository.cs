using RPG.Core.Domain.Entities;

namespace RPG.Core.Infrastructure.Repositories;

public interface ICharacterRepository
{
    PlayerCharacter GetById(Guid characterId);
}