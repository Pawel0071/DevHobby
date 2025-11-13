// filepath: /Volumes/Data/Repositories/DevHobby/RPG.GameServer/Dtos/NpcDto.cs
namespace RPG.GameServer.Dtos;

public sealed class NpcDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Level { get; init; }
    public bool IsMoving { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public float Rotation { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
}

