using System;
using RPG.Domain.Entities;

namespace RPG.Domain.Models;

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
