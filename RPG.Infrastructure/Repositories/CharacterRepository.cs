using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPG.Domain.Entities;
using RPG.Infrastructure.Interfaces;
using DomainCharacterRepository = RPG.Domain.Interfaces.ICharacterRepository;
using InfrastructureCharacterRepository = RPG.Infrastructure.Interfaces.ICharacterRepository;

namespace RPG.Infrastructure.Repositories;

public class CharacterRepository : DomainCharacterRepository, InfrastructureCharacterRepository
{
    private readonly IModelRepository _modelRepository;
    private readonly ILogger<CharacterRepository> _logger;

    public CharacterRepository(IModelRepository modelRepository, ILogger<CharacterRepository> logger)
    {
        _modelRepository = modelRepository;
        _logger = logger;
    }

    public async Task<Character> GetByIdAsync(Guid id)
    {
        var character = await _modelRepository.GetByIdAsync<Character>(id);
        if (character == null)
        {
            _logger.Warn($"Character {id} not found.");
            throw new KeyNotFoundException($"Character {id} not found.");
        }

        return character;
    }

    public async Task<Character> GetByNameAsync(string name)
    {
        var characters = await _modelRepository.GetAllAsync<Character>();
        var character = characters.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        if (character == null)
        {
            _logger.Warn($"Character '{name}' not found.");
            throw new KeyNotFoundException($"Character '{name}' not found.");
        }

        return character;
    }

    public async Task SaveAsync(Character character)
    {
        await UpsertAsync(character, "saved");
    }

    public Task<Character> CreateAsync(Character character)
    {
        return UpsertAsync(character, "created");
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var removed = await _modelRepository.DeleteAsync<Character>(id);
        if (!removed)
        {
            _logger.Warn($"Character {id} not found when attempting delete.");
        }

        return removed;
    }

    public async Task<Character?> GetAsync(string id)
    {
        if (Guid.TryParse(id, out var guid))
        {
            return await _modelRepository.GetByIdAsync<Character>(guid);
        }

        var characters = await _modelRepository.GetAllAsync<Character>();
        return characters.FirstOrDefault(c => string.Equals(c.Name, id, StringComparison.OrdinalIgnoreCase));
    }

    public Task<Character> UpdateAsync(Character character)
    {
        return UpsertAsync(character, "updated");
    }

    private async Task<Character> UpsertAsync(Character character, string action)
    {
        await _modelRepository.UpsertAsync(character);
        _logger.Debug($"Character {character.Id} {action}.");
        return character;
    }
}
