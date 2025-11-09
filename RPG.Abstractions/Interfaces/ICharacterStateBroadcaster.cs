using RPG.Domain.Models;

namespace RPG.Abstractions.Interfaces;

public interface ICharacterStateBroadcaster
{
    Task BroadcastAsync(CharacterStateUpdate update, CancellationToken cancellationToken = default);
    IReadOnlyCollection<CharacterStateSnapshot> GetSnapshots();
}
