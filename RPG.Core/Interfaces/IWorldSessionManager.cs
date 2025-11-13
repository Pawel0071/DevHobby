using System;
using System.Threading;
using System.Threading.Tasks;
using RPG.Domain.Models;

namespace RPG.Core.Interfaces;

public interface IWorldSessionManager
{
    Task<WorldJoinResult> JoinWorldAsync(Guid sessionId, Guid? preferredWorldId, CancellationToken cancellationToken);
    Task LeaveWorldAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<WorldState> GetWorldForSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<WorldState> GetWorldAsync(Guid worldId, CancellationToken cancellationToken);
    Task UpdateCharacterAsync(Guid sessionId, Location location, CancellationToken cancellationToken);
}

public sealed class WorldJoinResult
{
    public required WorldState World { get; init; }
    public required Location SpawnLocation { get; init; }
    public required GameSession Session { get; init; }
    public required Character Character { get; init; }
}
