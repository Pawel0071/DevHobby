namespace RPG.Domain.Models.Interaction;

public record CharacterStateSnapshot(
    Guid CharacterId,
    Location Location,
    bool IsMoving,
    bool IsRotating,
    float Rotation,
    DateTime Timestamp);
