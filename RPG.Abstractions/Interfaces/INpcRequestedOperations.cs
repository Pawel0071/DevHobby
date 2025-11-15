// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Abstractions/Interfaces/INpcRequestedOperations.cs
using System.Threading;
using System.Threading.Tasks;
using RPG.Abstractions.SharedModel;
using RPG.Domain.Models;

namespace RPG.Abstractions.Interfaces;

/// <summary>
/// Abstrakcja dla żądań NPC generowanych przez silnik AI/Core.
/// Implementacja w warstwie Application publikuje odpowiednie RequestedEvent-y.
/// Dzięki temu Core nie zależy od RPG.Application.
/// </summary>
public interface INpcRequestedOperations
{
    Task RequestMoveAsync(Guid npcId, Location destination, float speed = 1.0f, CancellationToken ct = default);
    Task RequestIdleAsync(Guid npcId, float durationSeconds = 0f, CancellationToken ct = default);
    Task RequestReturnToSpawnAsync(Guid npcId, CancellationToken ct = default);
    Task RequestUseSkillAsync(Guid npcId, Guid skillId, Guid? targetId, CancellationToken ct = default);
    Task RequestFollowAsync(Guid npcId, Guid targetId, float desiredRange, float stopDistance, float? maxRange, CancellationToken ct = default);
    Task RequestEngageAsync(Guid npcId, Guid targetCharacterId, CancellationToken ct = default);
    Task RequestDisengageAsync(Guid npcId, CancellationToken ct = default);
}
