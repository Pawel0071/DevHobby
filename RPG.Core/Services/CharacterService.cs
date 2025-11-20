using RPG.Core.Common;
using RPG.Core.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services;

public sealed class CharacterService : ICharacterService
{
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(ILogger<CharacterService> logger)
    {
        _logger = logger;
    }

    public Task<ServiceResult<bool>> HandleDeathAsync(Character character, CancellationToken ct = default)
    {
        if (character == null)
        {
            _logger.Warn("HandleDeathAsync called with null character");
            return Task.FromResult(false.ToResult());
        }

        // Set character to dead state (CurrentHealth = 0 represents death)
        character.CurrentHealth = 0;

        // TODO: Drop loot (iterate inventory, create WorldLootItems)
        // TODO: Set respawn timer (character.RespawnAt = DateTime.UtcNow.AddSeconds(30))
        // TODO: Clear combat state, buffs, etc.

        _logger.Info($"Character {character.Id} ({character.Name}) has died at {character.CurrentLocation}");

        return Task.FromResult(true.ToResult());
    }
}
