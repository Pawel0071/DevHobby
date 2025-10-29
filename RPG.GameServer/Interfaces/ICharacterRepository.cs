
using RPG.Domain.Entities;

namespace RPG.GameServer.Interfaces;

public interface ICharacterRepository
{
    Task<Character> CreateAsync(Character character);
    Task<bool> DeleteAsync(Guid id);
    Task<Character?> GetAsync(string id);
    Task<Character> UpdateAsync(Character character);
}