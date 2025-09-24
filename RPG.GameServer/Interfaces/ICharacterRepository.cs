using RPG.Core.Domain.Entities;

namespace RPG.GameServer.Interfaces;

public interface ICharacterRepository
{
    Task<PlayerCharacter> CreateAsync(PlayerCharacter character);
    Task<bool> DeleteAsync(string id);
    Task<PlayerCharacter?> GetAsync(string id);
    Task<PlayerCharacter> UpdateAsync(PlayerCharacter character);
}