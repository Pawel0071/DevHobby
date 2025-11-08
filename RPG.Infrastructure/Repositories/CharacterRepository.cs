using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPG.Domain.Entities;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories;

public class CharacterRepository : ICharacterRepository
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<CharacterRepository> _logger;

    public CharacterRepository(IDocumentRepository documentRepository, ILogger<CharacterRepository> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    public async Task<Character> GetByIdAsync(Guid id)
    {
        var character = await _documentRepository.GetByIdAsync<Character>(id);
        if (character == null)
        {
            _logger.Warn($"Character {id} not found.");
            throw new KeyNotFoundException($"Character {id} not found.");
        }

        return character;
    }

    public async Task<Character> GetByNameAsync(string name)
    {
        var characters = await _documentRepository.GetAllAsync<Character>();
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
        await _documentRepository.UpsertAsync(character);
        _logger.Debug($"Character {character.Id} saved.");
    }
}
