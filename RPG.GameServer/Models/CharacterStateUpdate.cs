using System;
using RPG.Domain.Entities;

namespace RPG.GameServer.Models;

public record CharacterStateUpdate(
    Guid CharacterId,
    Location? Location,
    bool? IsMoving = null,
    bool? IsRotating = null,
    float? Rotation = null,
    DateTime Timestamp = default);
