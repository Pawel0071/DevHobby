using RPG.Core.Domain.Entities;

namespace RPG.Core.Interfaces;

public interface ICharacterFactory
{
    T CreateCharacter<T>(string characterJson) where T : BaseCharacter;
}