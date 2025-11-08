using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPG.GameServer.Models;

namespace RPG.GameServer.Interfaces;

public interface ICharacterStateBroadcaster
{
    Task BroadcastAsync(CharacterStateUpdate update, CancellationToken cancellationToken = default);
    IReadOnlyCollection<CharacterStateSnapshot> GetSnapshots();
}
