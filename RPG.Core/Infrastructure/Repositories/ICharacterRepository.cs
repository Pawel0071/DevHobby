using RPG.Core.Domain.Entities;

namespace RPG.Core.Infrastructure.Repositories;

public interface ICharacterRepository
{
    Character GetById(Guid characterId);
}