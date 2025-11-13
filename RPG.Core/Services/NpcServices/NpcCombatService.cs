using System;
using System.Threading;
using System.Threading.Tasks;
using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Domain.Models;
using RPG.Domain.Models.Interaction;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Skills;
using RPG.Infrastructure.Interfaces;


namespace RPG.Core.Services.NpcServices;

/// <summary>
///     Bridges NPC combat directives with the broader game combat pipeline.
///     Persists combat telemetry and broadcasts domain events.
/// </summary>
public class NpcCombatService : INpcCombatService
{
    private readonly IGameEventDispatcher _eventDispatcher;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<NpcCombatService> _logger;
    private readonly IEventIdProvider _eventIdProvider;
    private readonly IEventSequenceStore _sequenceStore;

    public NpcCombatService(
        IGameEventDispatcher eventDispatcher,
        IRabbitMqPublisher publisher,
        ILogger<NpcCombatService> logger,
        IEventIdProvider eventIdProvider,
        IEventSequenceStore sequenceStore)
    {
        _eventDispatcher = eventDispatcher;
        _publisher = publisher;
        _logger = logger;
        _eventIdProvider = eventIdProvider;
        _sequenceStore = sequenceStore;
    }

    public async Task HandleSkillUsageAsync(
        Npc npc,
        Skill skill,
        Guid? targetCharacterId,
        CancellationToken cancellationToken = default)
    {
        if (npc == null)
        {
            throw new ArgumentNullException(nameof(npc));
        }

        if (skill == null)
        {
            throw new ArgumentNullException(nameof(skill));
        }

        var occurrence = DateTime.UtcNow;
        var location = CloneLocation(npc.CurrentLocation);


        var correlationId = Guid.NewGuid();
        var sequence = _sequenceStore.NextSequence(correlationId);
        var tempEvent = new NpcSkillUsedEvent(new EventMetadata(Guid.Empty, correlationId, null, sequence, occurrence), npc.Id,
            string.IsNullOrWhiteSpace(npc.DisplayName) ? npc.Name : npc.DisplayName,
            skill.Id,
            string.IsNullOrWhiteSpace(skill.Name) ? skill.Id.ToString("N") : skill.Name,
            targetCharacterId,
            location);
        var eventId = _eventIdProvider.Generate(tempEvent, occurrence, sequence, correlationId);
        var combatEvent = tempEvent with { Meta = tempEvent.Meta with { EventId = eventId } };

        await _eventDispatcher.DispatchAsync(combatEvent, cancellationToken).ConfigureAwait(false);

        var message = new NpcSkillUsageMessage(
            combatEvent.NpcId,
            combatEvent.NpcName,
            combatEvent.SkillId,
            combatEvent.SkillName,
            combatEvent.TargetCharacterId,
            location?.Position.X ?? 0f,
            location?.Position.Y ?? 0f,
            location?.Position.Z ?? 0f,
            location?.Rotation ?? 0f,
            occurrence);

        try
        {
            await _publisher.PublishAsync("combat.npc.skill", message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to publish NPC skill usage for NPC {npc.Id} and skill {skill.Id}", ex);
        }
    }

    private static Location? CloneLocation(Location? source)
    {
        if (source == null)
        {
            return null;
        }

        return new Location
        {
            Position = source.Position,
            Rotation = source.Rotation,
            MapId = source.MapId,
            ZoneName = source.ZoneName,
            WorldId = source.WorldId
        };
    }
}
