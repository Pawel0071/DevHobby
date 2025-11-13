using RPG.Domain.Models;
using RPG.Domain.Models.Interaction;

namespace RPG.Abstractions.Interfaces;

public interface ICharacterStateBroadcaster
{
    Task BroadcastAsync(CharacterStateUpdate update, CancellationToken cancellationToken = default);
    IReadOnlyCollection<CharacterStateSnapshot> GetSnapshots();
}
