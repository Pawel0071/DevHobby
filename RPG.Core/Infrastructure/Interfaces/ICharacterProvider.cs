using RPG.Core.Domain.Entities;

namespace RPG.Core.Infrastructure.Interfaces;

public interface ICharacterProvider
{
    Character GetById(Guid characterId);
}