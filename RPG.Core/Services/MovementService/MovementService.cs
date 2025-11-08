using System.Numerics;
using RPG.Core.Common;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Enums;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.MovementService;

public class MovementService : IMovementService
{
    private const float MinDirectionLengthSquared = 0.0001f;
    private readonly ILogger<MovementService> _logger;

    public MovementService(ILogger<MovementService> logger)
    {
        _logger = logger;
    }

    public ServiceResult<Location> Move(Character character, Vector3 direction, float deltaTime, float? speedOverride = null)
    {
        if (character == null)
        {
            return ErrorCodeDefinition.InvalidOperation.ToFail<Location>("Character is required for movement.");
        }

        return MoveInternal(
            entityType: "character",
            entityId: character.Id,
            location: character.CurrentLocation,
            stats: character.ModifiedStats,
            direction: direction,
            deltaTime: deltaTime,
            speedOverride: speedOverride);
    }

    public ServiceResult<Location> Move(Npc npc, Vector3 direction, float deltaTime, float? speedOverride = null)
    {
        if (npc == null)
        {
            return ErrorCodeDefinition.InvalidOperation.ToFail<Location>("NPC is required for movement.");
        }

        return MoveInternal(
            entityType: "npc",
            entityId: npc.Id,
            location: npc.CurrentLocation,
            stats: npc.ModifiedStats,
            direction: direction,
            deltaTime: deltaTime,
            speedOverride: speedOverride);
    }

    private ServiceResult<Location> MoveInternal(
        string entityType,
        Guid entityId,
        Location location,
        IDictionary<StatsProperty, int> stats,
        Vector3 direction,
        float deltaTime,
        float? speedOverride)
    {
        if (location == null)
        {
            _logger.Error($"{entityType} {entityId} has no location to update.");
            return ErrorCodeDefinition.InvalidOperation.ToFail<Location>("Brak lokalizacji dla ruchu.");
        }

        if (deltaTime <= 0f)
        {
            _logger.Warn($"Attempted to move {entityType} {entityId} with non-positive deltaTime: {deltaTime}.");
            return ErrorCodeDefinition.MovementDeltaInvalid.ToFail<Location>("Czas kroku ruchu musi być dodatni.");
        }

        var directionLengthSquared = direction.LengthSquared();
        if (directionLengthSquared < MinDirectionLengthSquared || float.IsNaN(directionLengthSquared))
        {
            _logger.Warn($"Attempted to move {entityType} {entityId} with invalid direction vector: {direction}.");
            return ErrorCodeDefinition.MovementInvalidDirection.ToFail<Location>("Kierunek ruchu jest niepoprawny.");
        }

        var effectiveSpeed = speedOverride ?? ResolveMoveSpeed(stats);
        if (effectiveSpeed <= 0f)
        {
            _logger.Warn($"{entityType} {entityId} has no movement speed (value: {effectiveSpeed}).");
            return ErrorCodeDefinition.MovementSpeedUnavailable.ToFail<Location>("Brak prędkości ruchu.");
        }

        var directionLength = MathF.Sqrt(directionLengthSquared);
        var normalizedDirection = direction / directionLength;
        var displacement = normalizedDirection * (effectiveSpeed * deltaTime);

        location.Position += displacement;
        UpdateFacing(location, normalizedDirection);

        _logger.Debug(
            $"Moved {entityType} {entityId} by {displacement} (speed={effectiveSpeed}, delta={deltaTime}). New position: {location.Position}");

        return ServiceResult<Location>.Ok(location);
    }

    private static float ResolveMoveSpeed(IDictionary<StatsProperty, int> stats)
    {
        if (stats == null)
        {
            return 0f;
        }

        return stats.TryGetValue(StatsProperty.MoveSpeed, out var moveSpeed)
            ? Math.Max(0, moveSpeed)
            : 0f;
    }

    private static void UpdateFacing(Location location, Vector3 direction)
    {
        if (direction.LengthSquared() < MinDirectionLengthSquared)
        {
            return;
        }

        var yawRadians = MathF.Atan2(direction.X, direction.Z);
        if (float.IsNaN(yawRadians))
        {
            return;
        }

        var yawDegrees = yawRadians * (180f / MathF.PI);
        location.Rotation = NormalizeAngle(yawDegrees);
    }

    private static float NormalizeAngle(float angle)
    {
        var normalized = angle % 360f;
        return normalized < 0f ? normalized + 360f : normalized;
    }
}
