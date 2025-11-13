namespace RPG.Domain.Models.Interaction;

public record CharacterStateUpdate(
    Guid CharacterId,
    Location? Location,
    bool? IsMoving = null,
    bool? IsRotating = null,
    float? Rotation = null,
    DateTime Timestamp = default);
