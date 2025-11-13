using RPG.Domain.Models;

namespace RPG.Domain.Interfaces;

public interface ICharacterRepository
{
    Task<Character> GetByIdAsync(Guid id);
    Task<Character> GetByNameAsync(string name);
    Task SaveAsync(Character character);
}
