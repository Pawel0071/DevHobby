using RPG.Domain.Entities;

namespace RPG.Domain.Interfaces;

public interface ICharacterProvider
{
    Character GetById(Guid characterId);
}