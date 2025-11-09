using System.Collections.Concurrent;
using RPG.Abstractions.Interfaces;
using RPG.Domain.Entities;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Broadcasters;

public class CharacterStateBroadcaster : ICharacterStateBroadcaster
{
    private readonly ILogger<CharacterStateBroadcaster> _logger;
    private readonly ConcurrentDictionary<Guid, CharacterStateSnapshot> _states = new();

    public CharacterStateBroadcaster(ILogger<CharacterStateBroadcaster> logger)
    {
        _logger = logger;
    }

    public Task BroadcastAsync(CharacterStateUpdate update, CancellationToken cancellationToken = default)
    {
        if (update == null)
        {
            _logger.Warn("Received null character state update.");
            return Task.CompletedTask;
        }

        var snapshot = _states.AddOrUpdate(
            update.CharacterId,
            _ => CreateSnapshot(update),
            (_, existing) => Merge(existing, update));

        _logger.Debug($"Updated state for character {snapshot.CharacterId} | moving={snapshot.IsMoving} rotating={snapshot.IsRotating} rotation={snapshot.Rotation}");

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<CharacterStateSnapshot> GetSnapshots()
    {
        return _states.Values.ToList();
    }

    private CharacterStateSnapshot CreateSnapshot(CharacterStateUpdate update)
    {
        var timestamp = update.Timestamp == default ? DateTime.UtcNow : update.Timestamp;
        var baseLocation = CloneLocation(update.Location) ?? new Location();
        var rotation = update.Rotation ?? baseLocation.Rotation;
        var isMoving = update.IsMoving ?? false;
        var isRotating = update.IsRotating ?? false;

        return new CharacterStateSnapshot(update.CharacterId, baseLocation, isMoving, isRotating, rotation, timestamp);
    }

    private CharacterStateSnapshot Merge(CharacterStateSnapshot existing, CharacterStateUpdate update)
    {
        var timestamp = update.Timestamp == default ? DateTime.UtcNow : update.Timestamp;
        var location = CloneLocation(update.Location) ?? CloneLocation(existing.Location) ?? new Location();
        var rotation = update.Rotation
                        ?? update.Location?.Rotation
                        ?? existing.Rotation;
        var isMoving = update.IsMoving ?? existing.IsMoving;
        var isRotating = update.IsRotating ?? existing.IsRotating;

        location.Rotation = rotation;

        return new CharacterStateSnapshot(update.CharacterId, location, isMoving, isRotating, rotation, timestamp);
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
