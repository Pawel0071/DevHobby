using System;
using RPG.Domain.Entities;

namespace RPG.Domain.Models;

public record CharacterStateSnapshot(
    Guid CharacterId,
    Location Location,
    bool IsMoving,
    bool IsRotating,
    float Rotation,
    DateTime Timestamp);
