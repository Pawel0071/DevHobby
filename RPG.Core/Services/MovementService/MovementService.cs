using System.Diagnostics;
using System.Numerics;
using RPG.Core.Common;
using RPG.Core.Diagnostics;
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

        using var activity = StartMovementActivity("MovementService.Move.Character", "character", character.Id);
        activity?.SetTag("rpg.movement.direction", direction.ToString());
        activity?.SetTag("rpg.movement.delta_time", deltaTime);
        if (speedOverride.HasValue)
        {
            activity?.SetTag("rpg.movement.speed_override", speedOverride.Value);
        }

        var result = MoveInternal(
            entityType: "character",
            entityId: character.Id,
            location: character.CurrentLocation,
            stats: character.ModifiedStats,
            direction: direction,
            deltaTime: deltaTime,
            speedOverride: speedOverride);

        if (result.Success)
        {
            character.SetMovementState(true);
        }

        return result;
    }

    public ServiceResult<Location> Move(Npc npc, Vector3 direction, float deltaTime, float? speedOverride = null)
    {
        if (npc == null)
        {
            return ErrorCodeDefinition.InvalidOperation.ToFail<Location>("NPC is required for movement.");
        }

        using var activity = StartMovementActivity("MovementService.Move.Npc", "npc", npc.Id);
        activity?.SetTag("rpg.movement.direction", direction.ToString());
        activity?.SetTag("rpg.movement.delta_time", deltaTime);
        if (speedOverride.HasValue)
        {
            activity?.SetTag("rpg.movement.speed_override", speedOverride.Value);
        }

        var result = MoveInternal(
            entityType: "npc",
            entityId: npc.Id,
            location: npc.CurrentLocation,
            stats: npc.ModifiedStats,
            direction: direction,
            deltaTime: deltaTime,
            speedOverride: speedOverride);

        if (result.Success)
        {
            npc.SetMovementState(true);
        }

        return result;
    }

    public ServiceResult<Location> Stop(Character character)
    {
        if (character == null)
        {
            return ErrorCodeDefinition.InvalidOperation.ToFail<Location>("Character is required for movement stop.");
        }

        using var activity = StartMovementActivity("MovementService.Stop.Character", "character", character.Id);

        var result = StopInternal("character", character.Id, character.CurrentLocation);
        if (result.Success)
        {
            character.SetMovementState(false);
        }

        return result;
    }

    public ServiceResult<Location> Stop(Npc npc)
    {
        if (npc == null)
        {
            return ErrorCodeDefinition.InvalidOperation.ToFail<Location>("NPC is required for movement stop.");
        }

        using var activity = StartMovementActivity("MovementService.Stop.Npc", "npc", npc.Id);

        var result = StopInternal("npc", npc.Id, npc.CurrentLocation);
        if (result.Success)
        {
            npc.SetMovementState(false);
        }

        return result;
    }

    public ServiceResult<float> Rotate(Character character, Vector3 direction)
    {
        if (character == null)
        {
            return ErrorCodeDefinition.InvalidOperation.ToFail<float>("Character is required for rotation.");
        }

        using var activity = StartMovementActivity("MovementService.Rotate.Character", "character", character.Id);
        activity?.SetTag("rpg.movement.direction", direction.ToString());

        var result = RotateInternal(
            entityType: "character",
            entityId: character.Id,
            location: character.CurrentLocation,
            direction: direction);

        if (result.Success)
        {
            character.SetRotationState(true);
        }

        return result;
    }

    public ServiceResult<float> Rotate(Npc npc, Vector3 direction)
    {
        if (npc == null)
        {
            return ErrorCodeDefinition.InvalidOperation.ToFail<float>("NPC is required for rotation.");
        }

        using var activity = StartMovementActivity("MovementService.Rotate.Npc", "npc", npc.Id);
        activity?.SetTag("rpg.movement.direction", direction.ToString());

        var result = RotateInternal(
            entityType: "npc",
            entityId: npc.Id,
            location: npc.CurrentLocation,
            direction: direction);

        if (result.Success)
        {
            npc.SetRotationState(true);
        }

        return result;
    }

    public ServiceResult<float> StopRotation(Character character)
    {
        if (character == null)
        {
            return ErrorCodeDefinition.InvalidOperation.ToFail<float>("Character is required to stop rotation.");
        }

        using var activity = StartMovementActivity("MovementService.StopRotation.Character", "character", character.Id);

        var result = StopRotationInternal("character", character.Id, character.CurrentLocation);
        if (result.Success)
        {
            character.SetRotationState(false);
        }

        return result;
    }

    public ServiceResult<float> StopRotation(Npc npc)
    {
        if (npc == null)
        {
            return ErrorCodeDefinition.InvalidOperation.ToFail<float>("NPC is required to stop rotation.");
        }

        using var activity = StartMovementActivity("MovementService.StopRotation.Npc", "npc", npc.Id);

        var result = StopRotationInternal("npc", npc.Id, npc.CurrentLocation);
        if (result.Success)
        {
            npc.SetRotationState(false);
        }

        return result;
    }

    private static Activity? StartMovementActivity(string operation, string entityType, Guid entityId)
    {
        var activity = CoreDiagnostics.ActivitySource.StartActivity(operation);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("rpg.entity.type", entityType);
        activity.SetTag("rpg.entity.id", entityId);
        return activity;
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

        if (!TryNormalizeDirection(direction, out var normalizedDirection))
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

        var displacement = normalizedDirection * (effectiveSpeed * deltaTime);

        location.Position += displacement;
        UpdateFacing(location, normalizedDirection);

        _logger.Debug(
            $"Moved {entityType} {entityId} by {displacement} (speed={effectiveSpeed}, delta={deltaTime}). New position: {location.Position}");

        return ServiceResult<Location>.Ok(location);
    }

    private ServiceResult<Location> StopInternal(string entityType, Guid entityId, Location location)
    {
        if (location == null)
        {
            _logger.Error($"{entityType} {entityId} has no location to stop movement.");
            return ErrorCodeDefinition.InvalidOperation.ToFail<Location>("Brak lokalizacji do zatrzymania ruchu.");
        }

        _logger.Debug($"Stopping movement for {entityType} {entityId} at position {location.Position}.");
        return ServiceResult<Location>.Ok(location);
    }

    private ServiceResult<float> RotateInternal(string entityType, Guid entityId, Location location, Vector3 direction)
    {
        if (location == null)
        {
            _logger.Error($"{entityType} {entityId} has no location to rotate.");
            return ErrorCodeDefinition.InvalidOperation.ToFail<float>("Brak lokalizacji do obrotu.");
        }

        if (!TryNormalizeDirection(direction, out var normalizedDirection))
        {
            _logger.Warn($"Attempted to rotate {entityType} {entityId} with invalid direction vector: {direction}.");
            return ErrorCodeDefinition.MovementInvalidDirection.ToFail<float>("Kierunek rotacji jest niepoprawny.");
        }

        var yawDegrees = CalculateYawDegrees(normalizedDirection);
        if (float.IsNaN(yawDegrees))
        {
            _logger.Warn($"Rotation for {entityType} {entityId} produced invalid yaw.");
            return ErrorCodeDefinition.MovementInvalidDirection.ToFail<float>("Nie udało się wyznaczyć rotacji.");
        }

        location.Rotation = yawDegrees;
        _logger.Debug($"Rotated {entityType} {entityId} to yaw {yawDegrees} degrees.");

        return ServiceResult<float>.Ok(yawDegrees);
    }

    private ServiceResult<float> StopRotationInternal(string entityType, Guid entityId, Location location)
    {
        if (location == null)
        {
            _logger.Error($"{entityType} {entityId} has no location to stop rotation.");
            return ErrorCodeDefinition.InvalidOperation.ToFail<float>("Brak lokalizacji do zatrzymania rotacji.");
        }

        _logger.Debug($"Stopping rotation for {entityType} {entityId} at yaw {location.Rotation}.");
        return ServiceResult<float>.Ok(location.Rotation);
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

    private static bool TryNormalizeDirection(Vector3 direction, out Vector3 normalizedDirection)
    {
        var directionLengthSquared = direction.LengthSquared();
        if (directionLengthSquared < MinDirectionLengthSquared || float.IsNaN(directionLengthSquared))
        {
            normalizedDirection = Vector3.Zero;
            return false;
        }

        var directionLength = MathF.Sqrt(directionLengthSquared);
        normalizedDirection = direction / directionLength;
        return true;
    }

    private static void UpdateFacing(Location location, Vector3 direction)
    {
        if (direction.LengthSquared() < MinDirectionLengthSquared)
        {
            return;
        }

        var yawDegrees = CalculateYawDegrees(direction);
        if (float.IsNaN(yawDegrees))
        {
            return;
        }

        location.Rotation = yawDegrees;
    }

    private static float CalculateYawDegrees(Vector3 normalizedDirection)
    {
        var yawRadians = MathF.Atan2(normalizedDirection.X, normalizedDirection.Z);
        if (float.IsNaN(yawRadians))
        {
            return float.NaN;
        }

        var yawDegrees = yawRadians * (180f / MathF.PI);
        return NormalizeAngle(yawDegrees);
    }

    private static float NormalizeAngle(float angle)
    {
        var normalized = angle % 360f;
        return normalized < 0f ? normalized + 360f : normalized;
    }
}
