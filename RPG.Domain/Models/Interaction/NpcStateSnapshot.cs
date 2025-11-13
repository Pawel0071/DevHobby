namespace RPG.Domain.Models.Interaction;

public sealed record NpcStateSnapshot(
    Guid NpcId,
    string Name,
    Location Location,
    bool IsMoving,
    bool IsRotating,
    float Rotation,
    DateTime Timestamp)
{
    public string DisplayName => Name;
}
