using RPG.Domain.Enums;

namespace RPG.Domain.Models.Interaction;

public record CharacterStateUpdate(
    Guid CharacterId,
    CharacterClass Class,
    Location? Location,
    bool? IsMoving = null,
    bool? IsRotating = null,
    float? Rotation = null,
    DateTime Timestamp = default);
