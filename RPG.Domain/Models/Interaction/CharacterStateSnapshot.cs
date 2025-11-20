using RPG.Domain.Enums;

namespace RPG.Domain.Models.Interaction;

public record CharacterStateSnapshot(
    Guid CharacterId,
    CharacterClass Class,
    Location Location,
    bool IsMoving,
    bool IsRotating,
    float Rotation,
    DateTime Timestamp);
