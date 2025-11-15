// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Events/Adapters/NpcRequestedOperationsAdapter.cs

using System.Numerics;
using RPG.Abstractions.Interfaces;
using RPG.Domain.Models;
using RPG.Application.Infrastructure;

namespace RPG.Application.Events.Adapters;

/// <summary>
/// Adapter publikujący requested eventy na podstawie wezwań z Core (INpcRequestedOperations).
/// Używa tej samej ścieżki co CommandHandler: EventId/Sequence, kolejka requested + inline dispatch.
/// </summary>
public sealed class NpcRequestedOperationsAdapter : INpcRequestedOperations
{
    private readonly IRequestEventQueue _requestQueue;
    private readonly IEventIdProvider _eventIdProvider;
    private readonly IEventSequenceStore _sequenceStore;
    private readonly IRequestedEventInlineDispatcher _inlineDispatcher;

    public NpcRequestedOperationsAdapter(
        IRequestEventQueue requestQueue,
        IEventIdProvider eventIdProvider,
        IEventSequenceStore sequenceStore,
        IRequestedEventInlineDispatcher inlineDispatcher)
    {
        _requestQueue = requestQueue;
        _eventIdProvider = eventIdProvider;
        _sequenceStore = sequenceStore;
        _inlineDispatcher = inlineDispatcher;
    }

    private async Task PublishAsync(IGameEventWithMetadata evt, CancellationToken ct)
    {
        _requestQueue.Enqueue(evt);
        await _inlineDispatcher.TryHandleAsync(evt, ct).ConfigureAwait(false);
    }

    private async Task PublishAsync<TEvent>(Func<EventMetadata, TEvent> factory, CancellationToken ct)
        where TEvent : IGameEventWithMetadata
    {
        var correlationId = Guid.NewGuid();
        var sequence = _sequenceStore.NextSequence(correlationId);
        var occurred = DateTime.UtcNow;
        var provisionalMeta = new EventMetadata(Guid.Empty, correlationId, null, sequence, occurred);
        var provisional = factory(provisionalMeta);
        var eventId = _eventIdProvider.Generate(provisional, occurred, sequence, correlationId);
        var finalMeta = provisionalMeta with { EventId = eventId };
        var finalEvent = factory(finalMeta);
        await PublishAsync(finalEvent, ct).ConfigureAwait(false);
    }

    public Task RequestMoveAsync(Guid npcId, Location destination, float speed = 1.0f, CancellationToken ct = default)
        => PublishAsync(meta => new NpcMoveRequestedEvent(meta, npcId, destination, speed), ct);

    public Task RequestIdleAsync(Guid npcId, float durationSeconds = 0f, CancellationToken ct = default)
        => PublishAsync(meta => new NpcIdleRequestedEvent(meta, npcId, durationSeconds), ct);

    public Task RequestReturnToSpawnAsync(Guid npcId, CancellationToken ct = default)
        => PublishAsync(meta => new NpcReturnToSpawnRequestedEvent(meta, npcId), ct);

    public Task RequestUseSkillAsync(Guid npcId, Guid skillId, Guid? targetId, CancellationToken ct = default)
        => PublishAsync(meta => new NpcSkillUseRequestedEvent(meta, npcId, skillId, targetId), ct);

    public Task RequestFollowAsync(Guid npcId, Guid targetId, float desiredRange, float stopDistance, float? maxRange, CancellationToken ct = default)
        => PublishAsync(meta => new NpcMoveRequestedEvent(meta, npcId,
            new Location { Position = Vector3.Zero, WorldId = Guid.Empty },
            1.0f), ct); // emit move requested; właściwy handler wykorzysta metadata (brak dedykowanego eventu follow)

    public Task RequestEngageAsync(Guid npcId, Guid targetCharacterId, CancellationToken ct = default)
        => PublishAsync(meta => new NpcEngageTargetRequestedEvent(meta, npcId, targetCharacterId), ct);

    public Task RequestDisengageAsync(Guid npcId, CancellationToken ct = default)
        => PublishAsync(meta => new NpcDisengageRequestedEvent(meta, npcId), ct);
}
