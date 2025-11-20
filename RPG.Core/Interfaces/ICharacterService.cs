using RPG.Core.Common;
using RPG.Domain.Models;

namespace RPG.Core.Interfaces;

public interface ICharacterService
{
    /// <summary>
    /// Handles character death logic: sets health to 0, drops loot, sets respawn timer, etc.
    /// </summary>
    Task<ServiceResult<bool>> HandleDeathAsync(Character character, CancellationToken ct = default);
}
