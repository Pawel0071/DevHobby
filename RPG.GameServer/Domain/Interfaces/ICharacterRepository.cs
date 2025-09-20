using RPG.GameServer.Protos;

namespace RPG.GameServer.Interfaces;

public interface ICharacterRepository
{
    Task<Character> CreateAsync(Character character);
    Task<Character?> GetAsync(string id);
    Task<Character> UpdateAsync(Character character);
    Task<bool> DeleteAsync(string id);
}